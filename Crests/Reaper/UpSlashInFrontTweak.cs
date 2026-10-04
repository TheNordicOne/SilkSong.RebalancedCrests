using BepInEx.Configuration;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Mirrors Reaper's up slash so its arc lies in front of Hornet instead of behind her (vanilla: 79.7 % of the
    /// hitbox area behind). The UpSlash object is rotated 270°, so its local y axis is the hero's x axis; negating
    /// NailSlash.scale.y (applied on every slash start) mirrors the hitbox, Clash Tink and effect sprite.
    /// </summary>
    internal sealed class UpSlashInFrontTweak : Tweak
    {
        internal UpSlashInFrontTweak(ConfigFile config)
            : base(config, Crest.Reaper, "UpSlashInFront",
                "Place the up slash in front of Hornet instead of behind her. Vanilla: mostly behind.")
        {
        }

        internal override void Apply(HeroController hero)
        {
            var upSlash = ReaperCrest.GetConfigGroup(hero).UpSlash;
            var scale = Override(upSlash, nameof(upSlash.scale), () => upSlash.scale, value => upSlash.scale = value);
            scale.Apply(Enabled.Value, Vector3.Scale(scale.Vanilla, new Vector3(1f, -1f, 1f)));
        }
    }
}
