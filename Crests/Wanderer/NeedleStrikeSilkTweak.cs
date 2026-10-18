using BepInEx.Configuration;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Every hit of Wanderer's multi-hit needle strike (Charge Slash Wanderer) gives silk. Vanilla: its DamageEnemies
    /// has silkGeneration = FirstHit, so only the first hit per target does.
    /// </summary>
    internal sealed class NeedleStrikeSilkTweak : Tweak
    {
        internal NeedleStrikeSilkTweak(ConfigFile config)
            : base(config, Crest.Wanderer, "Needle Strike - Silk on every hit",
                "Every hit of the Needle Strike gives Silk. Has no effect while 'Needle Strike - Single powerful swipe' "
                + "is on in Always mode; in FullHarpOnly mode 'Needle Strike - Silk every Nth hit' decides. "
                + "Vanilla: only the first hit gives Silk.")
        {
        }

        internal override void Apply(HeroController hero)
        {
            var damager = WandererCrest.GetConfigGroup(hero).ChargeSlash.GetComponent<DamageEnemies>();
            var silk = Override(damager, nameof(damager.silkGeneration), () => damager.silkGeneration, value => damager.silkGeneration = value);
            silk.Apply(Enabled.Value, HitSilkGeneration.Full);
        }
    }
}
