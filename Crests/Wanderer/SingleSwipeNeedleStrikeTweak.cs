using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using GlobalSettings;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Wanderer uses Hunter's needle strike (one big swing, Charge Slash Basic) instead of its multi-hit one.
    /// - Nail Arts "Antic Type": the Wanderer crest check is disabled, so the FSM takes the Hunter path (Antic →
    ///   Lunge? → Slash Recoil? → Do Slash). Wanderer's lunge speed and chain are 0 like Hunter's, and its
    ///   chargeSlashRecoils is False, so there is no pushback.
    /// - "Get Charge Slash": Current Charge Slash is set to Hunter's Charge Slash Basic.
    /// - HeroAnimationController.GetClip returns the base library "Slash_Charged" instead of the Dagger override, so
    ///   the hero animation and its clip triggers match Hunter's timing.
    /// - HealthManager.TakeDamage scales the damage of its hits.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SingleSwipeNeedleStrikeTweak : Tweak
    {
        private const string ChargedClip = "Slash_Charged";

        private static SingleSwipeNeedleStrikeTweak _instance;

        private GameObject _hunterChargeSlash;

        internal SingleSwipeNeedleStrikeTweak(ConfigFile config)
            : base(config, Crest.Wanderer, "Needle Strike - Single powerful swipe",
                "Replace the multi-hit Needle Strike with Hunter's single big swing. Vanilla: a flurry of weak hits.")
        {
            DamageMultiplier = config.Bind(Crest.Name, "Needle Strike - Swipe damage multiplier", 1f,
                new ConfigDescription("Damage multiplier of the single swipe. 1 = same damage as Hunter's Needle Strike.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            _instance = this;
        }

        private ConfigEntry<float> DamageMultiplier { get; }

        internal override void Apply(HeroController hero)
        {
            _hunterChargeSlash = hero.configs[0].ChargeSlash;

            var fsm = hero.GetFsm("Nail Arts");
            var anticType = fsm.GetState("Antic Type");
            if (anticType.HasAction<CallbackAction>(action => action.Tweak == this))
            {
                return;
            }

            var wandererCheck = anticType.GetAction<CheckIfCrestEquipped>(check => check.Crest.Value == Gameplay.WandererCrest);
            anticType.InsertActionBefore(anticType.Actions[0], new CallbackAction(this, _ => wandererCheck.Enabled = !IsActive));

            var currentChargeSlash = fsm.FsmVariables.GetFsmGameObject("Current Charge Slash");
            fsm.GetState("Get Charge Slash").AddAction(new CallbackAction(this, _ =>
            {
                if (IsActive)
                {
                    currentChargeSlash.Value = _hunterChargeSlash;
                    Core.DebugLog.Tweak(this, "Needle Strike: single swipe");
                }
            }));
        }

        private bool IsSwipeHit(HitInstance hit)
        {
            return _hunterChargeSlash && hit.Source && hit.Source.transform.IsChildOf(_hunterChargeSlash.transform);
        }

        [HarmonyPatch(typeof(HeroAnimationController), nameof(HeroAnimationController.GetClip))]
        [HarmonyPrefix]
        private static bool BaseChargedClip(HeroAnimationController __instance, string clipName, ref tk2dSpriteAnimationClip __result)
        {
            if (clipName != ChargedClip || !_instance.IsActive)
            {
                return true;
            }

            __result = __instance.animator.GetClipByName(clipName);
            return false;
        }

        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        private static void ScaleDamage(ref HitInstance hitInstance)
        {
            if (_instance.IsActive && _instance.IsSwipeHit(hitInstance))
            {
                hitInstance.DamageDealt = Mathf.RoundToInt(hitInstance.DamageDealt * _instance.DamageMultiplier.Value);
            }
        }
    }
}
