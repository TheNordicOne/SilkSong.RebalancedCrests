using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Runs Reaper's down slash through HeroController's generic Slash-type down attack (as Wanderer does) instead of
    /// the Crest Attacks FSM ("RPR DOWNSLASH"). That removes the 0.1667 s windup, the control lock
    /// (RelinquishControlNotVelocity until ~0.5 s) and the forced (30, -30) diagonal movement. The attack keeps
    /// Reaper's attack duration and cooldown, its hitbox object (Attacks/Scythe/DownSlash New, active for
    /// 0.1667 s) and the default pogo bounce. Vanilla, the bounce comes from the Crest Attacks FSM (Dealt Damage
    /// Response → ATTACK LANDED); on the Slash path a HeroDownAttack component does that, as on Wanderer's down slash,
    /// so one is added to the object while the tweak is enabled.
    /// </summary>
    [HarmonyPatch]
    internal sealed class DownSlashTweak : Tweak
    {
        private const string DownSlashObject = "DownSlash New";
        private const string VanillaBodyClip = "DownSlash";
        private const string ReaperBodyClip = "v3 Down Slash";

        private static DownSlashTweak _instance;

        internal DownSlashTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Down-slash - Instant, keeps control",
                "The Down-slash (pogo) hits the moment you press attack, you keep control of Hornet and she is no longer pulled "
                + "diagonally down. It keeps its slow attack speed. Vanilla: 0.1667 s windup, ~0.5 s without control.")
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

            var downAttack = downSlash.GetComponent<HeroDownAttack>();
            if (enabled && downAttack == null)
            {
                downSlash.AddComponent<HeroDownAttack>();
            }
            else if (!enabled && downAttack != null)
            {
                Remove(downAttack);
            }
        }

        /// <summary>
        /// HeroDownAttack subscribes to its attack's events in Awake and never unsubscribes, so that is done here.
        /// </summary>
        private static void Remove(HeroDownAttack downAttack)
        {
            var attack = downAttack.attack;
            if (attack != null)
            {
                attack.AttackStarting -= downAttack.OnAttackStarting;
                attack.EndedDamage -= downAttack.OnEndedDamage;
                attack.EnemyDamager.HitResponded -= downAttack.OnHitResponded;
            }

            Object.Destroy(downAttack);
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
