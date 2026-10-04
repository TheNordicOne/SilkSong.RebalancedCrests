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
        private ValueOverride<bool> _recoils;

        internal NeedleStrikePushbackTweak(ConfigFile config)
            : base(config, Crest.Hunter, "NeedleStrikeNoPushback",
                "Remove the backwards push during the needle strike, so Hornet stays in place. Vanilla: pushed back.")
        {
        }

        internal override void Apply(HeroController hero)
        {
            var config = HunterCrest.GetConfig(hero);
            _recoils ??= new ValueOverride<bool>(() => config.chargeSlashRecoils, value => config.chargeSlashRecoils = value);
            _recoils.Apply(Enabled.Value, false);
        }
    }
}
