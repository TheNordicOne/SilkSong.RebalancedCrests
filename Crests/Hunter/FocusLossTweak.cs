using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Hunter_v2/v3 lose a fixed amount of focus (combo meter hits) per hit taken instead of all of it.
    /// Vanilla calls ResetHunterUpgCrestState() from HeroController.TakeDamage and DoSpecialDamage; those two calls
    /// are redirected. Every other reset (death, crest/tool changes, respawn, maggots) stays a full reset.
    /// </summary>
    [HarmonyPatch]
    internal sealed class FocusLossTweak : Tweak
    {
        private static FocusLossTweak _instance;

        internal FocusLossTweak(ConfigFile config)
            : base(config, HunterCrest.Section, "FocusLossOnHit",
                "Lose only some focus when taking damage with the upgraded Hunter crest. Vanilla: all focus is lost.")
        {
            Amount = config.Bind(HunterCrest.Section, "FocusLossOnHitAmount", 6,
                new ConfigDescription(
                    "Focus lost per hit taken, regardless of masks lost or double damage.",
                    new AcceptableValueRange<int>(0, 18)));
            _instance = this;
        }

        internal ConfigEntry<int> Amount { get; }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(HeroController), nameof(HeroController.TakeDamage));
            yield return AccessTools.Method(typeof(HeroController), "DoSpecialDamage");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var reset = AccessTools.Method(typeof(HeroController), "ResetHunterUpgCrestState");
            var replacement = AccessTools.Method(typeof(FocusLossTweak), nameof(OnHeroDamaged));
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
                throw new InvalidOperationException($"Expected one focus reset in {original.Name}, found {replaced}");
            }
        }

        private static void OnHeroDamaged(HeroController hero)
        {
            if (_instance == null || !_instance.Enabled.Value)
            {
                hero.ResetHunterUpgCrestState();
                return;
            }

            hero.hunterUpgState.CurrentMeterHits = Math.Max(0, hero.hunterUpgState.CurrentMeterHits - _instance.Amount.Value);
        }
    }
}
