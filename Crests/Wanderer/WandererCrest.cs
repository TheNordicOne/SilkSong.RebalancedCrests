using System.Linq;
using GlobalSettings;

namespace RebalancedCrests.Crests.Wanderer
{
    internal static class WandererCrest
    {
        internal static HeroController.ConfigGroup GetConfigGroup(HeroController hero)
        {
            var config = Gameplay.WandererCrest.HeroConfig;
            return hero.configs.First(group => group.Config == config);
        }
    }
}
