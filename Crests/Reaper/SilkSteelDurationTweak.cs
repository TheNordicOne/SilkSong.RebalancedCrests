using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Silk Steel (reaper mode) is no longer cancelled by taking damage; instead every hit on an enemy that would drop
    /// silk bundles shortens the remaining duration. Vanilla: HeroController.TakeDamage calls ResetReaperCrestState()
    /// on any damage; that single call is redirected.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkSteelDurationTweak : Tweak
    {
        private static SilkSteelDurationTweak _instance;

        internal SilkSteelDurationTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steel - Kept on damage",
                "Taking damage no longer ends Silk Steel; each successful hit shortens it instead. "
                + "Vanilla: taking damage ends it.")
        {
            SecondsPerHit = config.Bind(Crest.Name, "Silk Steel - Seconds lost per hit", 1f,
                new ConfigDescription("Seconds of Silk Steel lost per successful hit. Vanilla duration: 10 seconds.",
                    new AcceptableValueRange<float>(0f, 10f)));
            _instance = this;
        }

        private ConfigEntry<float> SecondsPerHit { get; }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.TakeDamage))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> KeepOnDamage(IEnumerable<CodeInstruction> instructions)
        {
            var reset = AccessTools.Method(typeof(HeroController), "ResetReaperCrestState");
            var replacement = AccessTools.Method(typeof(SilkSteelDurationTweak), nameof(OnHeroDamaged));
            var replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(reset))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }

                yield return instruction;
            }

            if (replaced != 1)
            {
                throw new InvalidOperationException($"Expected one Silk Steel reset in TakeDamage, found {replaced}");
            }
        }

        private static void OnHeroDamaged(HeroController hero)
        {
            if (_instance == null || !_instance.IsActive)
            {
                hero.ResetReaperCrestState();
            }
        }

        /// <summary>
        /// Same hit conditions as the vanilla bundle drop in HealthManager.TakeDamage (nail hits and nail-tagged heavy
        /// hits that generate silk, on enemies that give silk).
        /// </summary>
        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPostfix]
        private static void ShortenOnHit(HealthManager __instance, HitInstance hitInstance)
        {
            var hero = HeroController.instance;
            if (_instance == null || !_instance.IsActive || hero == null || !hero.reaperState.IsInReaperMode
                || __instance.enemyType == HealthManager.EnemyTypes.Shade
                || __instance.enemyType == HealthManager.EnemyTypes.Armoured
                || __instance.DoNotGiveSilk)
            {
                return;
            }

            var isNailHit = hitInstance.AttackType == AttackTypes.Nail
                            || (hitInstance.AttackType == AttackTypes.Heavy && hitInstance.IsNailTag);
            if (!isNailHit || hitInstance.SilkGeneration == HitSilkGeneration.None)
            {
                return;
            }

            hero.reaperState.ReaperModeDurationLeft -= _instance.SecondsPerHit.Value;
            if (hero.reaperState.ReaperModeDurationLeft <= 0f)
            {
                hero.ResetReaperCrestState();
            }
        }
    }
}
