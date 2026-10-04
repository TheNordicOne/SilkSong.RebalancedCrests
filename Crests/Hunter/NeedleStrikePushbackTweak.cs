using BepInEx.Configuration;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Removes the backwards push of Hunter's needle strike. The Nail Arts FSM calls
    /// HeroController.ChargeSlashRecoilRight/Left in "Regain Partial Control", which only recoils when
    /// Config.ChargeSlashRecoils is set (true for Default/Hunter).
    /// </summary>
    internal sealed class NeedleStrikePushbackTweak : Tweak
    {
        internal NeedleStrikePushbackTweak(ConfigFile config)
            : base(config, Crest.Hunter, "Needle strike - No pushback",
                "Remove the backwards push during the needle strike, so Hornet stays in place. Vanilla: pushed back.")
        {
        }

        internal override void Apply(HeroController hero)
        {
            var config = HunterCrest.GetConfig(hero);
            Override(config, nameof(config.chargeSlashRecoils), () => config.chargeSlashRecoils, value => config.chargeSlashRecoils = value)
                .Apply(Enabled.Value, false);
        }
    }
}
