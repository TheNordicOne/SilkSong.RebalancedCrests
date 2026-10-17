using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Replaces the random 1-3 Silk fragments (35/50/15 %) dropped per hit during Silk Steal (Reaper bind effect, reaper mode) with a fixed count.
    /// </summary>
    internal sealed class FixedSilkBundlesTweak : Tweak
    {
        internal FixedSilkBundlesTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Fixed fragments per hit",
                "During Silk Steal (Reaper's bind effect), every hit drops the same number of Silk fragments. Vanilla: random 1-3 (average 1.8). Enemies that always drop 1 fragment are not changed.")
        {
            Count = config.Bind(Crest.Name, "Silk Steal - Fragments per hit", 2,
                new ConfigDescription("Silk fragments per hit. 3 fragments = 1 Silk.", new AcceptableValueRange<int>(0, 9)));
            ReaperPayoutPatch.Fixed = this;
        }

        internal ConfigEntry<int> Count { get; }
    }

    /// <summary>
    /// Makes needle strike hits drop more Silk fragments during Silk Steal than other hits.
    /// </summary>
    internal sealed class NeedleStrikeSilkBundlesTweak : Tweak
    {
        internal NeedleStrikeSilkBundlesTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - More Needle Strike fragments",
                "During Silk Steal, Needle Strike hits drop more Silk fragments. Vanilla: same as any other hit.")
        {
            Count = config.Bind(Crest.Name, "Silk Steal - Needle Strike fragments per hit", 6,
                new ConfigDescription("Silk fragments per Needle Strike hit. 3 fragments = 1 Silk.",
                    new AcceptableValueRange<int>(0, 18)));
            ReaperPayoutPatch.NeedleStrike = this;
        }

        internal ConfigEntry<int> Count { get; }
    }

    /// <summary>
    /// HeroController.GetReaperPayout() decides the bundle count of a hit (called from HealthManager.TakeDamage for
    /// enemies with the Normal bundle tier) but does not know the hit, so it is taken from <see cref="EnemyHitContext"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class ReaperPayoutPatch
    {
        internal static FixedSilkBundlesTweak Fixed;
        internal static NeedleStrikeSilkBundlesTweak NeedleStrike;

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.GetReaperPayout))]
        [HarmonyPrefix]
        private static bool OverridePayout(HeroController __instance, ref int __result)
        {
            var hit = EnemyHitContext.Current;
            if (hit == null || !__instance.ReaperState.IsInReaperMode)
            {
                return true;
            }

            if (hit.Value.AttackType == AttackTypes.Heavy && NeedleStrike != null && NeedleStrike.IsActive)
            {
                __result = NeedleStrike.Count.Value;
                DebugLog.Tweak(NeedleStrike, $"Silk Steal: Needle Strike hit drops {__result} fragments");
                return false;
            }

            if (Fixed != null && Fixed.IsActive)
            {
                __result = Fixed.Count.Value;
                DebugLog.Tweak(Fixed, $"Silk Steal: hit drops {__result} fragments");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs hits on enemies whose bundle tier is not Normal: they never call GetReaperPayout (Reduced always drops
        /// 1 fragment, None drops 0), so the tweaks do not apply to them.
        /// </summary>
        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        private static void LogBundleTier(HealthManager __instance)
        {
            if (Fixed != null && Fixed.IsActive && HeroController.instance.ReaperState.IsInReaperMode
                && __instance.reaperBundles != HealthManager.ReaperBundleTiers.Normal)
            {
                DebugLog.Tweak(Fixed, $"Silk Steal: '{__instance.name}' has bundle tier {__instance.reaperBundles}, not changed");
            }
        }
    }
}
