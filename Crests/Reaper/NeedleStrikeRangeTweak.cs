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
            : base(config, Crest.Reaper, "Needle Strike - Bigger", "Make the Needle Strike bigger so it reaches further.")
        {
            _scale = config.Bind(Crest.Name, "Needle Strike - Size multiplier", 1.25f,
                new ConfigDescription("Size multiplier of the Needle Strike. Vanilla: 1.",
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
