using BepInEx.Configuration;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Shortens the windup (antic) of Hunter's down attack before its hitbox becomes active.
    /// Vanilla waits downspikeAnticTime (0.1 s). With 0 the hitbox starts in the frame the attack is pressed,
    /// because HeroController.Update checks the antic timer after handling input.
    /// </summary>
    internal sealed class PogoWindupTweak : Tweak
    {
        private readonly ConfigEntry<float> _windupSeconds;

        internal PogoWindupTweak(ConfigFile config)
            : base(config, Crest.Hunter, "Down-slash - Shorter windup",
                "Change the windup of the Down-slash (pogo) before it can hit. Vanilla: 0.1 seconds.")
        {
            _windupSeconds = config.Bind(Crest.Name, "Down-slash - Windup seconds", 0f,
                new ConfigDescription(
                    "Windup of the Down-slash in seconds. 0 = it hits the moment you press attack.",
                    new AcceptableValueRange<float>(0f, 0.5f)));
        }

        internal override void Apply(HeroController hero)
        {
            var config = HunterCrest.GetConfig(hero);
            Override(config, nameof(config.downspikeAnticTime), () => config.downspikeAnticTime, value => config.downspikeAnticTime = value)
                .Apply(IsEnabled, _windupSeconds.Value);
        }
    }
}
