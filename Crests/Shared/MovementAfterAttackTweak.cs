using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Shared
{
    /// <summary>
    /// Allows dash, Drifter's Cloak and double jump during and right after a nail attack. Vanilla blocks them while
    /// cState.attacking and attack_time &lt; Config.AttackRecoveryTime (CanDash inline, CanDoubleJump and CanFloat via
    /// IsAttackLocked). Attack, Clawline, silk skills, tools and bind keep their vanilla locks.
    /// </summary>
    [HarmonyPatch]
    internal sealed class MovementAfterAttackTweak : Tweak
    {
        private static readonly List<MovementAfterAttackTweak> Instances = new List<MovementAfterAttackTweak>();

        internal MovementAfterAttackTweak(ConfigFile config, Crest crest)
            : base(config, crest, "MovementAfterAttack",
                "Allow dash, Drifter's Cloak and double jump during and right after an attack. "
                + "Vanilla: blocked until the attack's recovery time has passed.")
        {
            Instances.Add(this);
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(HeroController), nameof(HeroController.CanDash));
            yield return AccessTools.Method(typeof(HeroController), nameof(HeroController.CanDoubleJump));
            yield return AccessTools.Method(typeof(HeroController), nameof(HeroController.CanFloat));
        }

        /// <summary>
        /// These checks only see the attack through cState.attacking, so it is hidden for the duration of the check.
        /// </summary>
        private static void Prefix(HeroController __instance, out bool __state)
        {
            __state = __instance.cState.attacking && Instances.Any(tweak => tweak.IsActive);
            if (__state)
            {
                __instance.cState.attacking = false;
            }
        }

        private static Exception Finalizer(HeroController __instance, bool __state, Exception __exception)
        {
            if (__state)
            {
                __instance.cState.attacking = true;
            }

            return __exception;
        }
    }
}
