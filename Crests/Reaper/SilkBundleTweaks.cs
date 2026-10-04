using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Replaces the random 1-3 silk bundles (35/50/15 %) dropped per hit in Silk Steel (reaper mode) with a fixed count.
    /// </summary>
    internal sealed class FixedSilkBundlesTweak : Tweak
    {
        internal FixedSilkBundlesTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steel - Fixed bundles per hit",
                "Drop a fixed number of silk bundles per hit during Silk Steel. Vanilla: random 1-3 (average 1.8).")
        {
            Count = config.Bind(Crest.Name, "Silk Steel - Bundles per hit", 2,
                new ConfigDescription("Silk bundles per hit. 3 bundles = 1 silk.", new AcceptableValueRange<int>(0, 9)));
            ReaperPayoutPatch.Fixed = this;
        }

        internal ConfigEntry<int> Count { get; }
    }

    /// <summary>
    /// Makes needle strike hits drop more silk bundles during Silk Steel than other hits.
    /// </summary>
    internal sealed class NeedleStrikeSilkBundlesTweak : Tweak
    {
        internal NeedleStrikeSilkBundlesTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steel - More needle strike bundles",
                "Needle strike hits drop more silk bundles during Silk Steel. Vanilla: same as any other hit.")
        {
            Count = config.Bind(Crest.Name, "Silk Steel - Needle strike bundles per hit", 6,
                new ConfigDescription("Silk bundles per needle strike hit. 3 bundles = 1 silk.",
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
                return false;
            }

            if (Fixed != null && Fixed.IsActive)
            {
                __result = Fixed.Count.Value;
                return false;
            }

            return true;
        }
    }
}
