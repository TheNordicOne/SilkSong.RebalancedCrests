using BepInEx.Configuration;
using RebalancedCrests.Crests;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// A single, individually configurable gameplay change. Each tweak owns its config entries
    /// (grouped by crest section) and re-applies itself whenever the hero starts or a setting changes.
    /// </summary>
    internal abstract class Tweak
    {
        protected Tweak(ConfigFile config, Crest crest, string name, string description)
        {
            Crest = crest;
            Enabled = config.Bind(crest.Name, $"{name}Enabled", true, description);
        }

        internal Crest Crest { get; }

        /// <summary>
        /// True while the tweak is enabled and its crest is equipped.
        /// </summary>
        internal bool IsActive => Enabled.Value && Crest.IsEquipped;

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
