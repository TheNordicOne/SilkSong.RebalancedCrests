using System;
using System.Collections.Generic;
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
        protected Tweak(ConfigFile config, Crest crest, string key, string description)
        {
            Crest = crest;
            Enabled = config.Bind(crest.Name, key, true, description);
        }

        /// <summary>
        /// For tweaks that are switched by another setting (e.g. a mode) instead of their own toggle; they override
        /// <see cref="IsOwnEnabled"/>.
        /// </summary>
        protected Tweak(Crest crest)
        {
            Crest = crest;
        }

        internal Crest Crest { get; }

        /// <summary>
        /// True while the tweak is enabled and its crest is equipped.
        /// </summary>
        internal bool IsActive => IsEnabled && Crest.IsEquipped;

        /// <summary>
        /// True while the tweak's own setting is on and 'Mod - Enable tweaks' is on.
        /// </summary>
        internal bool IsEnabled => AllTweaks.Value && IsOwnEnabled;

        protected virtual bool IsOwnEnabled => Enabled.Value;

        /// <summary>
        /// The General master switch; off disables every tweak.
        /// </summary>
        internal static ConfigEntry<bool> AllTweaks { get; private set; }

        internal static void BindGeneral(ConfigFile config)
        {
            AllTweaks = config.Bind("General", "Mod - Enable tweaks", true,
                "Turn every tweak on or off at once. Off: all crests play like vanilla.");
        }

        internal ConfigEntry<bool> Enabled { get; }

        /// <summary>
        /// Applies the tweak to the current hero, or restores vanilla behaviour when disabled.
        /// Tweaks that read their config live (e.g. from Harmony patches) have nothing to do here.
        /// </summary>
        internal virtual void Apply(HeroController hero)
        {
        }

        private readonly Dictionary<(object Target, string Key), object> _overrides = new Dictionary<(object, string), object>();

        /// <summary>
        /// Returns the override of a value on <paramref name="target"/>, capturing its vanilla value on first use.
        /// Overrides are cached per target, because hero objects are recreated when a save is loaded while assets
        /// such as hero configs persist (and must keep their first captured vanilla value).
        /// </summary>
        protected ValueOverride<T> Override<T>(object target, string key, Func<T> get, Action<T> set)
        {
            if (!_overrides.TryGetValue((target, key), out var cached))
            {
                cached = new ValueOverride<T>(get, set);
                _overrides[(target, key)] = cached;
            }

            return (ValueOverride<T>)cached;
        }
    }
}
