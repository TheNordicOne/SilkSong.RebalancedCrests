using System;
using HarmonyLib;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Hero lifecycle hooks shared by tweaks and diagnostics.
    /// </summary>
    [HarmonyPatch]
    internal static class HeroEvents
    {
        /// <summary>
        /// Raised after <see cref="HeroController"/> has started, i.e. its configs and attack objects exist.
        /// </summary>
        internal static event Action<HeroController> HeroStarted;

        [HarmonyPatch(typeof(HeroController), "Start")]
        [HarmonyPostfix]
        private static void AfterHeroStart(HeroController __instance)
        {
            HeroStarted?.Invoke(__instance);
        }
    }
}
