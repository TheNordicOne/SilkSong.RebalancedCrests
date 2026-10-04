using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Runs Reaper's down slash through HeroController's generic Slash-type down attack (as Wanderer does) instead of
    /// the Crest Attacks FSM ("RPR DOWNSLASH"). That removes the 0.1667 s windup, the control lock
    /// (RelinquishControlNotVelocity until ~0.5 s) and the forced (30, -30) diagonal movement. The attack keeps
    /// Reaper's attack duration and cooldown, its hitbox object (Attacks/Scythe/DownSlash New, active for
    /// 0.1667 s), its HeroDownAttack pogo detection and the default pogo bounce.
    /// </summary>
    [HarmonyPatch]
    internal sealed class DownSlashTweak : Tweak
    {
        private const string DownSlashObject = "DownSlash New";
        private const string VanillaBodyClip = "DownSlash";
        private const string ReaperBodyClip = "v3 Down Slash";

        private static DownSlashTweak _instance;

        internal DownSlashTweak(ConfigFile config)
            : base(config, Crest.Reaper, "DownSlashResponsive",
                "Down slash (pogo) without windup, without locking Hornet's controls and without the forced diagonal "
                + "movement. It keeps its slow attack speed. Vanilla: 0.1667 s windup, ~0.5 s without control.")
        {
            _instance = this;
        }

        internal override void Apply(HeroController hero)
        {
            var group = ReaperCrest.GetConfigGroup(hero);
            var config = group.Config;
            var downSlash = group.ActiveRoot.transform.Find(DownSlashObject).gameObject;

            var enabled = Enabled.Value;
            Override(config, nameof(config.downSlashType), () => config.downSlashType, value => config.downSlashType = value)
                .Apply(enabled, HeroControllerConfig.DownSlashTypes.Slash);
            Override(group, nameof(group.DownSlashObject), () => group.DownSlashObject, value => group.DownSlashObject = value)
                .Apply(enabled, downSlash);
            Override(group, nameof(group.DownSlash), () => group.DownSlash, value => group.DownSlash = value)
                .Apply(enabled, downSlash.GetComponent<NailSlash>());
            Override(group, nameof(group.DownSlashDamager), () => group.DownSlashDamager, value => group.DownSlashDamager = value)
                .Apply(enabled, downSlash.GetComponent<DamageEnemies>());
        }

        /// <summary>
        /// Slash-type down attacks play the hero clip "DownSlash", which neither the Scythe library nor the base
        /// library has; Reaper's own down slash body clip is used instead.
        /// </summary>
        [HarmonyPatch(typeof(HeroAnimationController), "Play")]
        [HarmonyPrefix]
        private static void UseReaperBodyClip(ref string clipName)
        {
            if (clipName == VanillaBodyClip && _instance != null && _instance.IsActive)
            {
                clipName = ReaperBodyClip;
            }
        }
    }
}
