using BepInEx.Configuration;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Shortens the windup (antic) of Hunter's down attack before its hitbox becomes active.
    /// Vanilla waits downspikeAnticTime (0.1 s); with 0 the hitbox starts in the frame the attack is pressed,
    /// because HeroController.Update checks the antic timer after handling input.
    /// </summary>
    internal sealed class PogoWindupTweak : Tweak
    {
        private readonly ConfigEntry<float> _windupSeconds;

        internal PogoWindupTweak(ConfigFile config)
            : base(config, Crest.Hunter, "Pogo - Shorter windup",
                "Change the windup of the down attack (pogo) before its hitbox becomes active. Vanilla: 0.1 seconds.")
        {
            _windupSeconds = config.Bind(Crest.Name, "Pogo - Windup seconds", 0f,
                new ConfigDescription(
                    "Windup of the down attack in seconds. 0 = the hitbox is active in the frame the attack is pressed.",
                    new AcceptableValueRange<float>(0f, 0.5f)));
        }

        internal override void Apply(HeroController hero)
        {
            var config = HunterCrest.GetConfig(hero);
            Override(config, nameof(config.downspikeAnticTime), () => config.downspikeAnticTime, value => config.downspikeAnticTime = value)
                .Apply(Enabled.Value, _windupSeconds.Value);
        }
    }
}
