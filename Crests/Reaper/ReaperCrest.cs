using System.Linq;
using GlobalSettings;

namespace RebalancedCrests.Crests.Reaper
{
    internal static class ReaperCrest
    {
        internal static HeroController.ConfigGroup GetConfigGroup(HeroController hero)
        {
            var config = Gameplay.ReaperCrest.HeroConfig;
            return hero.configs.First(group => group.Config == config);
        }
    }
}
