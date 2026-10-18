using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using GlobalSettings;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// The Needle Strike of the Wanderer rework modes (CriticalStrikeTweak.Mode): a single, scaled-up copy of Hunter's
    /// normal slash instead of the multi-hit one. CriticalStrike mode always uses it; HarpNeedleStrike mode only while
    /// the harp is full. Each mode has its own settings.
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
    /// - "Get Charge Slash" (before "Antic Type") decides per Needle Strike whether the swipe is used.
    /// - HarpNeedleStrike mode, multi-hit Needle Strike: a HeroController.SilkGain prefix gives Silk on every Nth hit of
    ///   the strike (instead of silkGeneration FirstHit, which gives 1 per enemy), and a HeroController.NailHitEnemy
    ///   postfix adds harp strings once per strike that hits an enemy. "Wanderer Attack" resets these counters.
    /// - HarpNeedleStrike mode, swipe: the SilkGain prefix lets it give at most 1 Silk per strike (or none). Its
    ///   Hunter slash copy has silkGeneration Full, which in CriticalStrike mode gives 1 Silk per enemy hit.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SingleSwipeNeedleStrikeTweak : Tweak
    {
        private const string ChargedClip = "Slash_Charged";
        private const float NeedleStrikeDamage = 2.8f;

        private static SingleSwipeNeedleStrikeTweak _instance;

        private GameObject _swipe;
        private NailSlash _swipeSlash;
        private GameObject _flurry;
        private bool _useSwipe;
        private int _flurryHits;
        private bool _flurryCharged;
        private bool _swipeSilkGiven;

        internal SingleSwipeNeedleStrikeTweak(ConfigFile config)
            : base(Crest.Wanderer)
        {
            CriticalStrikeDamage = BindDamage(config, "Critical Strike");
            CriticalStrikeSize = BindSize(config, "Critical Strike");
            HarpDamage = BindDamage(config, "Harp Needle Strike");
            HarpSize = BindSize(config, "Harp Needle Strike");
            FlurrySilkEvery = config.Bind(Crest.Name, "Harp Needle Strike - Silk every Nth hit", 2,
                new ConfigDescription("Every Nth hit of the multi-hit Needle Strike gives Silk (1 = every hit). "
                    + "Vanilla: only the first hit per enemy.",
                    new AcceptableValueRange<int>(1, 5)));
            FlurryHarpStrings = config.Bind(Crest.Name, "Harp Needle Strike - Harp charge per strike", 2,
                new ConfigDescription("Harp strings added by a multi-hit Needle Strike that hits an enemy.",
                    new AcceptableValueRange<int>(0, 50)));
            SwipeGivesSilk = config.Bind(Crest.Name, "Harp Needle Strike - Swipe gives 1 Silk", true,
                "The full-harp swipe gives 1 Silk when it hits. Off: it gives none.");
            _instance = this;
        }

        private ConfigEntry<float> CriticalStrikeDamage { get; }

        private ConfigEntry<float> CriticalStrikeSize { get; }

        private ConfigEntry<float> HarpDamage { get; }

        private ConfigEntry<float> HarpSize { get; }

        private ConfigEntry<int> FlurrySilkEvery { get; }

        private ConfigEntry<int> FlurryHarpStrings { get; }

        private ConfigEntry<bool> SwipeGivesSilk { get; }

        private static CriticalStrikeTweak Harp => CriticalStrikeTweak.Instance;

        internal override bool IsEnabled => Harp.IsEnabled;

        private float DamageMultiplier => (Harp.IsHarpNeedleStrike ? HarpDamage : CriticalStrikeDamage).Value;

        private float SizeMultiplier => (Harp.IsHarpNeedleStrike ? HarpSize : CriticalStrikeSize).Value;

        private static ConfigEntry<float> BindDamage(ConfigFile config, string mode)
        {
            return config.Bind(Crest.Wanderer.Name, mode + " - Swipe damage multiplier", 1f,
                new ConfigDescription("Damage multiplier of the single swipe. 1 = same damage as Hunter's Needle Strike.",
                    new AcceptableValueRange<float>(0.5f, 3f)));
        }

        private static ConfigEntry<float> BindSize(ConfigFile config, string mode)
        {
            return config.Bind(Crest.Wanderer.Name, mode + " - Swipe size multiplier", 1.5f,
                new ConfigDescription("Size of the single swipe compared to Hunter's normal slash.",
                    new AcceptableValueRange<float>(1f, 3f)));
        }

        internal override void Apply(HeroController hero)
        {
            if (!_swipe)
            {
                CreateSwipe(hero);
            }

            _flurry = WandererCrest.GetConfigGroup(hero).ChargeSlash;

            var fsm = hero.GetFsm("Nail Arts");
            var anticType = fsm.GetState("Antic Type");
            if (anticType.HasAction<CallbackAction>(action => action.Tweak == this))
            {
                return;
            }

            var wandererCheck = anticType.GetAction<CheckIfCrestEquipped>(check => check.Crest.Value == Gameplay.WandererCrest);
            anticType.InsertActionBefore(anticType.Actions[0], new CallbackAction(this, _ => wandererCheck.Enabled = !_useSwipe));

            var currentChargeSlash = fsm.FsmVariables.GetFsmGameObject("Current Charge Slash");
            fsm.GetState("Get Charge Slash").AddAction(new CallbackAction(this, _ =>
            {
                _useSwipe = IsActive && (!Harp.IsHarpNeedleStrike || Harp.HarpFull);
                if (_useSwipe)
                {
                    _swipe.transform.localScale = Vector3.one * SizeMultiplier;
                    currentChargeSlash.Value = _swipe;
                    Core.DebugLog.Tweak(this, "Needle Strike: single swipe");
                }
            }));

            fsm.GetState("Do Slash").AddAction(new CallbackAction(this, _ =>
            {
                if (_useSwipe && currentChargeSlash.Value == _swipe)
                {
                    _swipeSilkGiven = false;
                    _swipeSlash.StartSlash();
                }
            }));

            fsm.GetState("Wanderer Attack").AddAction(new CallbackAction(this, _ =>
            {
                _flurryHits = 0;
                _flurryCharged = false;
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

        private bool IsFlurryHit(HitInstance hit)
        {
            return !_useSwipe && _flurry && hit.Source == _flurry;
        }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.SilkGain), typeof(HitInstance))]
        [HarmonyPrefix]
        private static void HarpNeedleStrikeSilk(ref HitInstance hitInstance)
        {
            var tweak = _instance;
            if (!Harp.IsHarpNeedleStrike)
            {
                return;
            }

            bool gives;
            if (tweak.IsFlurryHit(hitInstance))
            {
                gives = tweak._flurryHits++ % tweak.FlurrySilkEvery.Value == 0;
            }
            else if (tweak.IsSwipeHit(hitInstance))
            {
                gives = tweak.SwipeGivesSilk.Value && !tweak._swipeSilkGiven;
                tweak._swipeSilkGiven |= gives;
            }
            else
            {
                return;
            }

            hitInstance.SilkGeneration = gives ? HitSilkGeneration.Full : HitSilkGeneration.None;
        }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.NailHitEnemy))]
        [HarmonyPostfix]
        private static void FlurryChargesHarp(HealthManager enemyHealth, HitInstance hitInstance)
        {
            var tweak = _instance;
            if (!Harp.IsHarpNeedleStrike || tweak._flurryCharged || enemyHealth.DoNotGiveSilk
                || !tweak.IsFlurryHit(hitInstance))
            {
                return;
            }

            tweak._flurryCharged = true;
            Harp.AddStrings(tweak.FlurryHarpStrings.Value);
        }

        [HarmonyPatch(typeof(HeroAnimationController), nameof(HeroAnimationController.GetClip))]
        [HarmonyPrefix]
        private static bool BaseChargedClip(HeroAnimationController __instance, string clipName, ref tk2dSpriteAnimationClip __result)
        {
            if (clipName != ChargedClip || !_instance.IsActive || !_instance._useSwipe)
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
                hitInstance.DamageDealt = Mathf.RoundToInt(hitInstance.DamageDealt * _instance.DamageMultiplier);
            }
        }
    }
}
