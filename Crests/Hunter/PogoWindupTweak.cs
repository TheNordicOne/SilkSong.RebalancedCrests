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
        private ValueOverride<float> _anticTime;

        internal PogoWindupTweak(ConfigFile config)
            : base(config, Crest.Hunter, "PogoWindup",
                "Change the windup of the down attack (pogo) before its hitbox becomes active. Vanilla: 0.1 seconds.")
        {
            _windupSeconds = config.Bind(Crest.Name, "PogoWindupSeconds", 0f,
                new ConfigDescription(
                    "Windup of the down attack in seconds. 0 = the hitbox is active in the frame the attack is pressed.",
                    new AcceptableValueRange<float>(0f, 0.5f)));
        }

        internal override void Apply(HeroController hero)
        {
            var config = HunterCrest.GetConfig(hero);
            _anticTime ??= new ValueOverride<float>(() => config.downspikeAnticTime, value => config.downspikeAnticTime = value);
            _anticTime.Apply(Enabled.Value, _windupSeconds.Value);
        }
    }
}
