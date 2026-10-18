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
    /// - Charge Slash Basic has two hitboxes, "slash 01" (0–0.083 s) and "slash 02" (0.217–0.3 s), each 1.4× nail
    ///   damage. With 'Swipe hits once', DamageEnemies.DoDamage is skipped for "slash 02" and "slash 01" deals the
    ///   damage of both. The second blade swing is hidden (SecondSwingHider) and its sound ("Audio Player Actor (1)",
    ///   PlayRandomAudioEvent on enable) is skipped.
    /// - HealthManager.TakeDamage scales the damage of its hits.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SingleSwipeNeedleStrikeTweak : Tweak
    {
        private const string ChargedClip = "Slash_Charged";

        private static SingleSwipeNeedleStrikeTweak _instance;

        private GameObject _hunterChargeSlash;
        private DamageEnemies _firstHit;
        private DamageEnemies _secondHit;
        private PlayRandomAudioEvent _secondSound;

        internal SingleSwipeNeedleStrikeTweak(ConfigFile config)
            : base(config, Crest.Wanderer, "Needle Strike - Single powerful swipe",
                "Replace the multi-hit Needle Strike with Hunter's single big swing. Vanilla: a flurry of weak hits.")
        {
            DamageMultiplier = config.Bind(Crest.Name, "Needle Strike - Swipe damage multiplier", 1f,
                new ConfigDescription("Damage multiplier of the single swipe. 1 = same damage as Hunter's Needle Strike.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            HitsOnce = config.Bind(Crest.Name, "Needle Strike - Swipe hits once", true,
                "The swipe hits once with the damage of both hits. Vanilla (Hunter's Needle Strike): two hits about "
                + "0.2 s apart.");
            _instance = this;
        }

        private ConfigEntry<float> DamageMultiplier { get; }

        private ConfigEntry<bool> HitsOnce { get; }

        internal bool HitsOnceActive => IsActive && HitsOnce.Value;

        internal override void Apply(HeroController hero)
        {
            _hunterChargeSlash = hero.configs[0].ChargeSlash;
            _firstHit = _hunterChargeSlash.transform.Find("slash 01").GetComponent<DamageEnemies>();
            _secondHit = _hunterChargeSlash.transform.Find("slash 02").GetComponent<DamageEnemies>();
            _secondSound = _hunterChargeSlash.transform.Find("Audio Player Actor (1)").GetComponent<PlayRandomAudioEvent>();
            if (!_hunterChargeSlash.GetComponent<SecondSwingHider>())
            {
                _hunterChargeSlash.AddComponent<SecondSwingHider>().Tweak = this;
            }

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

        [HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.DoDamage), typeof(GameObject), typeof(bool))]
        [HarmonyPrefix]
        private static bool SkipSecondHit(DamageEnemies __instance, ref bool __result)
        {
            if (__instance != _instance._secondHit || !_instance.HitsOnceActive)
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(PlayRandomAudioEvent), "OnEnable")]
        [HarmonyPrefix]
        private static bool SkipSecondSound(PlayRandomAudioEvent __instance)
        {
            return __instance != _instance._secondSound || !_instance.HitsOnceActive;
        }

        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        private static void ScaleDamage(ref HitInstance hitInstance)
        {
            if (!_instance.IsActive || !_instance.IsSwipeHit(hitInstance))
            {
                return;
            }

            var multiplier = _instance.DamageMultiplier.Value;
            if (_instance.HitsOnceActive && hitInstance.Source == _instance._firstHit.gameObject)
            {
                multiplier *= 2;
            }

            hitInstance.DamageDealt = Mathf.RoundToInt(hitInstance.DamageDealt * multiplier);
        }
    }
}
