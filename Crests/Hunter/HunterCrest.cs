namespace RebalancedCrests.Crests.Hunter
{
    internal static class HunterCrest
    {
        internal const string Section = "Hunter";

        /// <summary>
        /// Hunter, Hunter_v2 and Hunter_v3 have no hero config of their own; the hero falls back to
        /// configs[0] ("Default"), see HeroController.UpdateConfig.
        /// </summary>
        internal static HeroControllerConfig GetConfig(HeroController hero) => hero.configs[0].Config;
    }
}
