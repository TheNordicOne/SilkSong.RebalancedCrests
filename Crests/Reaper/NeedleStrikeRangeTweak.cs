using BepInEx.Configuration;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Enlarges Reaper's needle strike (Attacks/Charge Slash Scythe, vanilla localScale (1.3, 1.3, 1)) including its
    /// three hitboxes. The object is inactive at rest; KeepWorldPosition captures the scale again on every enable.
    /// </summary>
    internal sealed class NeedleStrikeRangeTweak : Tweak
    {
        private readonly ConfigEntry<float> _scale;

        internal NeedleStrikeRangeTweak(ConfigFile config)
            : base(config, Crest.Reaper, "NeedleStrikeRange", "Enlarge the needle strike and its hitboxes.")
        {
            _scale = config.Bind(Crest.Name, "NeedleStrikeRangeScale", 1.25f,
                new ConfigDescription("Size multiplier of the needle strike. Vanilla: 1.",
                    new AcceptableValueRange<float>(1f, 2f)));
        }

        internal override void Apply(HeroController hero)
        {
            var chargeSlash = ReaperCrest.GetConfigGroup(hero).ChargeSlash.transform;
            var localScale = Override(chargeSlash, nameof(chargeSlash.localScale), () => chargeSlash.localScale, value => chargeSlash.localScale = value);
            localScale.Apply(Enabled.Value, localScale.Vanilla * _scale.Value);
        }
    }
}
