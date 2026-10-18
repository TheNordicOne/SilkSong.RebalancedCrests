using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using GlobalSettings;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Wanderer's Needle Strike is a single, scaled-up copy of Hunter's normal slash instead of its multi-hit one.
    /// - Nail Arts "Antic Type": the Wanderer crest check is disabled, so the FSM takes the Hunter path (Antic →
    ///   Lunge? → Slash Recoil? → Do Slash). Wanderer's lunge speed and chain are 0 like Hunter's, and its
    ///   chargeSlashRecoils is False, so there is no pushback.
    /// - HeroAnimationController.GetClip returns the base library "Slash_Charged" instead of the Dagger override, so
    ///   the hero animation and its clip triggers match Hunter's timing.
    /// - The swipe is "Wanderer Swipe" under Attacks: a parent with HeroExtraNailSlash (so the game and the other
    ///   tweaks treat its hits as Needle Strike hits, and it takes the nail imbuement) and a copy of Hunter's
    ///   "Attacks/Default/Slash" as child. The parent is scaled, because NailAttackBase.OnSlashStarting resets the
    ///   slash's own scale. The copy is Heavy with the damage of both hits of Hunter's Needle Strike (2 × 1.4 nail
    ///   damage), and has no NailSlashRecoil, so hits do not push Hornet back.
    /// - "Get Charge Slash": Current Charge Slash is set to the swipe, so "Do Slash" activates it and "Cancel All"
    ///   deactivates it. An action added to "Do Slash" starts the slash (NailSlash.StartSlash).
    /// - HealthManager.TakeDamage scales the damage of its hits.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SingleSwipeNeedleStrikeTweak : Tweak
    {
        private const string ChargedClip = "Slash_Charged";
        private const float NeedleStrikeDamage = 2.8f;

        private static SingleSwipeNeedleStrikeTweak _instance;

        private GameObject _swipe;
        private NailSlash _swipeSlash;

        internal SingleSwipeNeedleStrikeTweak(ConfigFile config)
            : base(config, Crest.Wanderer, "Needle Strike - Single powerful swipe",
                "Replace the multi-hit Needle Strike with a single big swing: Hunter's normal slash, scaled up. "
                + "Vanilla: a flurry of weak hits.")
        {
            DamageMultiplier = config.Bind(Crest.Name, "Needle Strike - Swipe damage multiplier", 1f,
                new ConfigDescription("Damage multiplier of the single swipe. 1 = same damage as Hunter's Needle Strike.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            SizeMultiplier = config.Bind(Crest.Name, "Needle Strike - Swipe size multiplier", 1.5f,
                new ConfigDescription("Size of the single swipe compared to Hunter's normal slash.",
                    new AcceptableValueRange<float>(1f, 3f)));
            _instance = this;
        }

        private ConfigEntry<float> DamageMultiplier { get; }

        private ConfigEntry<float> SizeMultiplier { get; }

        internal override void Apply(HeroController hero)
        {
            if (!_swipe)
            {
                CreateSwipe(hero);
            }

            _swipe.transform.localScale = Vector3.one * SizeMultiplier.Value;

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
                    currentChargeSlash.Value = _swipe;
                    Core.DebugLog.Tweak(this, "Needle Strike: single swipe");
                }
            }));

            fsm.GetState("Do Slash").AddAction(new CallbackAction(this, _ =>
            {
                if (IsActive && currentChargeSlash.Value == _swipe)
                {
                    _swipeSlash.StartSlash();
                }
            }));
        }

        private void CreateSwipe(HeroController hero)
        {
            var hunterSlash = hero.configs[0].NormalSlashObject;
            _swipe = new GameObject("Wanderer Swipe");
            _swipe.SetActive(false);
            _swipe.transform.SetParent(hunterSlash.transform.parent.parent, false);
            _swipe.transform.localPosition = hunterSlash.transform.parent.localPosition;

            var slash = Object.Instantiate(hunterSlash, _swipe.transform, false);
            slash.name = hunterSlash.name;
            slash.SetActive(true);
            foreach (var recoil in slash.GetComponents<NailSlashRecoil>())
            {
                Object.DestroyImmediate(recoil);
            }

            _swipeSlash = slash.GetComponent<NailSlash>();
            _swipeSlash.dontAddRecoiler = true;
            _swipeSlash.setSlashComponent = false;
            var damager = slash.GetComponent<DamageEnemies>();
            damager.attackType = AttackTypes.Heavy;
            damager.nailDamageMultiplier = NeedleStrikeDamage;

            // HeroExtraNailSlash collects the damagers in Awake, so it is added after the slash
            var extraSlash = _swipe.AddComponent<HeroExtraNailSlash>();
            extraSlash.tintSprites = new SpriteRenderer[0];
            extraSlash.tintTk2dSprites = new tk2dSprite[0];
        }

        private bool IsSwipeHit(HitInstance hit)
        {
            return _swipe && hit.Source && hit.Source.transform.IsChildOf(_swipe.transform);
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
