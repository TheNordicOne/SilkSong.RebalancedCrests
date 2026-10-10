using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Critical Strike: replaces Wanderer's random critical hits (2 % per nail hit at 9+ Silk) with a harp that fills
    /// with nail hits once Needle Strike is obtained. A full harp makes the next Needle Strike a critical hit on every
    /// hit (vanilla crit: 3× damage, 1.5× knockback, Wanderer_Precision_strike effect).
    /// - Hits are counted in HeroController.NailHitEnemy with the same conditions as Hunter's combo meter; Needle
    ///   Strike hits do not count.
    /// - IsWandererLucky drives both the random crit roll (DamageEnemies.DoDamage) and the vanilla harp on the HUD
    ///   (BindOrbHudFrame). It returns false inside the roll and "harp full" everywhere else, so the HUD harp appears
    ///   when the harp is full.
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
            : base(config, Crest.Wanderer, "Critical hits - Critical Strike",
                "Replace the random critical hits with Critical Strike: once you have Needle Strike, nail hits fill the "
                + "harp (shown on the HUD when full) and a full harp makes your next Needle Strike a critical hit "
                + "(3× damage). Vanilla: 2 % chance per nail hit while at 9 or more Silk.")
        {
            HitsToFill = config.Bind(Crest.Name, "Critical Strike - Hits to fill harp", 7,
                new ConfigDescription("Nail hits on enemies needed to fill the harp.",
                    new AcceptableValueRange<int>(1, 50)));
            _instance = this;
        }

        private ConfigEntry<int> HitsToFill { get; }

        private bool HarpFull => _strings >= HitsToFill.Value;

        internal override void Apply(HeroController hero)
        {
            // Wanderer's own Needle Strike, and Hunter's when 'Needle Strike - Single powerful swipe' is on
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
