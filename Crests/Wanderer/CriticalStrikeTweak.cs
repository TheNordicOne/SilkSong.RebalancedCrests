using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Wanderer rework modes ('Rework - Mode'): CriticalStrike and HarpNeedleStrike both replace Wanderer's random
    /// critical hits (2 % per nail hit at 9+ Silk) with a harp that fills with nail hits once Needle Strike is
    /// obtained. A full harp makes the next Needle Strike a critical hit on every hit (vanilla crit: 3× damage, 1.5×
    /// knockback, Wanderer_Precision_strike effect). The Needle Strike itself per mode: SingleSwipeNeedleStrikeTweak.
    /// Each mode has its own harp size.
    /// - Hits are counted in HeroController.NailHitEnemy with the same conditions as Hunter's combo meter; Needle
    ///   Strike hits do not count.
    /// - IsWandererLucky drives both the random crit roll (DamageEnemies.DoDamage) and the vanilla harp on the HUD
    ///   (BindOrbHudFrame). It returns false inside the roll and "harp full" everywhere else, so the HUD harp appears
    ///   when the harp is full.
    /// - While the harp fills, the HUD shows the first frames of the harp's Full animation (HarpHudDisplay).
    /// - The Nail Arts states that activate the Needle Strike ("Wanderer Attack", or "Do Slash" with the single
    ///   swipe) consume a full harp. "Do Slash" is shared with other crests; the action only acts with Wanderer.
    /// - HealthManager.TakeDamage marks the hits of that Needle Strike as critical.
    /// </summary>
    [HarmonyPatch]
    internal sealed class CriticalStrikeTweak : Tweak
    {
        private static CriticalStrikeTweak _instance;
        private static bool _inCritRoll;

        private int _strings;
        private bool _criticalNeedleStrike;

        internal CriticalStrikeTweak(ConfigFile config)
            : base(Crest.Wanderer)
        {
            Mode = config.Bind(Crest.Name, "Rework - Mode", WandererMode.CriticalStrike,
                "Which Wanderer rework is used. "
                + "Vanilla: random critical hits (2 % chance per nail hit while at 9 or more Silk) and the multi-hit "
                + "Needle Strike. "
                + "CriticalStrike: Needle Strike is a single powerful swipe; nail hits fill the harp and a full harp "
                + "makes the next swipe a critical hit (3× damage). Uses the 'Critical Strike - ...' settings. "
                + "HarpNeedleStrike: the multi-hit Needle Strike is used until the harp is full; nail hits and "
                + "Needle Strikes fill the harp, and a full harp turns the next Needle Strike into the critical "
                + "swipe. Uses the 'Harp Needle Strike - ...' settings.");
            CriticalStrikeHitsToFill = config.Bind(Crest.Name, "Critical Strike - Hits to fill harp", 7,
                new ConfigDescription("Nail hits on enemies needed to fill the harp.",
                    new AcceptableValueRange<int>(1, 50)));
            HarpNeedleStrikeHitsToFill = config.Bind(Crest.Name, "Harp Needle Strike - Hits to fill harp", 7,
                new ConfigDescription("Harp strings needed to fill the harp; each nail hit on an enemy adds one.",
                    new AcceptableValueRange<int>(1, 50)));
            _instance = this;
        }

        internal enum WandererMode
        {
            Vanilla,
            CriticalStrike,
            HarpNeedleStrike,
        }

        internal ConfigEntry<WandererMode> Mode { get; }

        private ConfigEntry<int> CriticalStrikeHitsToFill { get; }

        private ConfigEntry<int> HarpNeedleStrikeHitsToFill { get; }

        private ConfigEntry<int> HitsToFill =>
            Mode.Value == WandererMode.HarpNeedleStrike ? HarpNeedleStrikeHitsToFill : CriticalStrikeHitsToFill;

        internal override bool IsEnabled => Mode.Value != WandererMode.Vanilla;

        internal bool IsHarpNeedleStrike => IsActive && Mode.Value == WandererMode.HarpNeedleStrike;

        internal static CriticalStrikeTweak Instance => _instance;

        internal bool HarpFull => _strings >= HitsToFill.Value;

        /// <summary>
        /// Stage of a harp that is filling: 0 for the first half of the hits, 1 for the second half, -1 when the
        /// harp is empty or full. Shown on the HUD by <see cref="HarpHudDisplay"/>.
        /// </summary>
        internal int FillingStage => _strings == 0 || HarpFull ? -1 : _strings * 2 / HitsToFill.Value;

        internal override void Apply(HeroController hero)
        {
            // Wanderer's own multi-hit Needle Strike, and the single swipe
            foreach (var stateName in new[] { "Wanderer Attack", "Do Slash" })
            {
                var state = hero.GetFsm("Nail Arts").GetState(stateName);
                if (!state.HasAction<CallbackAction>(action => action.Tweak == this))
                {
                    state.AddAction(new CallbackAction(this, _ => OnNeedleStrike()));
                }
            }
        }

        /// <summary>
        /// Runs when the Needle Strike is activated: a full harp is consumed and makes this strike critical.
        /// </summary>
        private void OnNeedleStrike()
        {
            _criticalNeedleStrike = IsActive && HarpFull;
            if (_criticalNeedleStrike)
            {
                _strings = 0;
                DebugLog.Tweak(this, "Critical Strike: Needle Strike is critical");
            }
        }

        /// <summary>
        /// Adds strings to the harp, up to full.
        /// </summary>
        internal void AddStrings(int count)
        {
            _strings = Mathf.Min(_strings + count, HitsToFill.Value);
            DebugLog.Tweak(this, $"Critical Strike harp {_strings}/{HitsToFill.Value}");
        }

        private static bool IsNeedleStrike(HitInstance hit)
        {
            return hit.Source && hit.Source.GetComponentInParent<HeroExtraNailSlash>();
        }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.NailHitEnemy))]
        [HarmonyPostfix]
        private static void CountHit(HeroController __instance, HealthManager enemyHealth, HitInstance hitInstance)
        {
            var tweak = _instance;
            if (!tweak.IsActive || !PlayerData.instance.hasChargeSlash || __instance.cState.isMaggoted
                || hitInstance.RageHit || hitInstance.SilkGeneration == HitSilkGeneration.None
                || enemyHealth.DoNotGiveSilk || IsNeedleStrike(hitInstance) || tweak.HarpFull)
            {
                return;
            }

            tweak._strings++;
            DebugLog.Tweak(tweak, $"Critical Strike harp {tweak._strings}/{tweak.HitsToFill.Value}");
        }

        [HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.DoDamage), typeof(UnityEngine.GameObject), typeof(bool))]
        [HarmonyPrefix]
        private static void EnterCritRoll() => _inCritRoll = true;

        [HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.DoDamage), typeof(UnityEngine.GameObject), typeof(bool))]
        [HarmonyFinalizer]
        private static System.Exception LeaveCritRoll(System.Exception __exception)
        {
            _inCritRoll = false;
            return __exception;
        }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.IsWandererLucky), MethodType.Getter)]
        [HarmonyPostfix]
        private static void Lucky(ref bool __result)
        {
            if (_instance.IsActive)
            {
                __result = !_inCritRoll && _instance.HarpFull;
            }
        }

        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        private static void CriticalNeedleStrike(ref HitInstance hitInstance)
        {
            if (_instance._criticalNeedleStrike && _instance.IsActive && IsNeedleStrike(hitInstance))
            {
                hitInstance.CriticalHit = true;
            }
        }
    }
}
