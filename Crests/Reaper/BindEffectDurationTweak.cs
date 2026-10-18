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
    /// Silk Steal (Reaper bind effect, reaper mode) is no longer cancelled by taking damage, so it always lasts its full
    /// duration (Gameplay.ReaperModeDuration, 10 s). Vanilla: HeroController.TakeDamage calls ResetReaperCrestState()
    /// on any damage; that single call is redirected.
    /// </summary>
    [HarmonyPatch]
    internal sealed class BindEffectDurationTweak : Tweak
    {
        private static BindEffectDurationTweak _instance;

        internal BindEffectDurationTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Kept on damage",
                "Taking damage no longer ends Silk Steal (Reaper's bind effect); it always lasts its full 10 seconds. "
                + "Vanilla: taking damage ends it.")
        {
            _instance = this;
        }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.TakeDamage))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> KeepOnDamage(IEnumerable<CodeInstruction> instructions)
        {
            var reset = AccessTools.Method(typeof(HeroController), "ResetReaperCrestState");
            var replacement = AccessTools.Method(typeof(BindEffectDurationTweak), nameof(OnHeroDamaged));
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
                throw new InvalidOperationException($"Expected one Silk Steal reset in TakeDamage, found {replaced}");
            }
        }

        private static void OnHeroDamaged(HeroController hero)
        {
            if (_instance == null || !_instance.IsActive)
            {
                hero.ResetReaperCrestState();
            }
        }
    }
}
