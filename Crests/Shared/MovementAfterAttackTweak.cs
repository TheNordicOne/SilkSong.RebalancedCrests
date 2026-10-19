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
    /// One instance per crest. Only the instance of the equipped crest applies.
    /// </summary>
    [HarmonyPatch]
    internal sealed class MovementAfterAttackTweak : Tweak
    {
        private static readonly List<MovementAfterAttackTweak> Instances = new List<MovementAfterAttackTweak>();

        internal MovementAfterAttackTweak(ConfigFile config, Crest crest)
            : base(config, crest, "Attacking - Allow dash, glide and double jump",
                "Allow dash, gliding (Drifter's Cloak) and double jump during and right after an attack. "
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
        private static void Prefix(HeroController __instance, out MovementAfterAttackTweak __state)
        {
            __state = __instance.cState.attacking ? Instances.FirstOrDefault(tweak => tweak.IsActive) : null;
            if (__state != null)
            {
                __instance.cState.attacking = false;
            }
        }

        private static (string Method, int Attack) _lastLogged;

        private static void Postfix(HeroController __instance, MovementAfterAttackTweak __state, bool __result, MethodBase __originalMethod)
        {
            // only checks made during an attack, once per check and attack
            if (!__instance.cState.attacking && __state == null)
            {
                return;
            }

            var key = (__originalMethod.Name, __instance.cState.attackCount);
            if (key == _lastLogged)
            {
                return;
            }

            _lastLogged = key;
            var tweak = Instances.FirstOrDefault(t => t.Crest.IsEquipped);
            if (tweak != null)
            {
                DebugLog.Tweak(tweak, $"{__originalMethod.Name} checked {__instance.attack_time:0.000}s into an attack "
                                      + $"(vanilla lock: {__instance.Config.AttackRecoveryTime:0.000}s): "
                                      + $"tweak {(__state != null ? "active" : "inactive")}, result {__result}");
            }
        }

        private static Exception Finalizer(HeroController __instance, MovementAfterAttackTweak __state, Exception __exception)
        {
            if (__state != null)
            {
                __instance.cState.attacking = true;
            }

            return __exception;
        }
    }
}
