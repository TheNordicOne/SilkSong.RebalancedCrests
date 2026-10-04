using BepInEx.Configuration;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// A single, individually configurable gameplay change. Each tweak owns its config entries
    /// (grouped by crest section) and re-applies itself whenever the hero starts or a setting changes.
    /// </summary>
    internal abstract class Tweak
    {
        protected Tweak(ConfigFile config, string section, string name, string description)
        {
            Enabled = config.Bind(section, $"{name}Enabled", true, description);
        }

        internal ConfigEntry<bool> Enabled { get; }

        /// <summary>
        /// Applies the tweak to the current hero, or restores vanilla behaviour when disabled.
        /// Tweaks that read their config live (e.g. from Harmony patches) have nothing to do here.
        /// </summary>
        internal virtual void Apply(HeroController hero)
        {
        }
    }
}
