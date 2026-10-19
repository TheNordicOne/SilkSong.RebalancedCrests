using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Changes how long Silk Steal (reaper mode) lasts. HeroController.BindCompleted starts it with
    /// ReaperModeDurationLeft = Gameplay.ReaperModeDuration (10 s). A postfix sets the configured duration instead.
    /// Harvest Bind also starts Silk Steal through BindCompleted.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealDurationTweak : Tweak
    {
        private static SilkStealDurationTweak _instance;

        internal SilkStealDurationTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Custom duration",
                "Silk Steal lasts 'Silk Steal - Duration seconds'. Vanilla: 10 seconds.")
        {
            Seconds = config.Bind(Crest.Name, "Silk Steal - Duration seconds", 10f,
                new ConfigDescription("How long Silk Steal lasts. Vanilla: 10 seconds.",
                    new AcceptableValueRange<float>(1f, 30f)));
            _instance = this;
        }

        private ConfigEntry<float> Seconds { get; }

        [HarmonyPatch(typeof(HeroController), nameof(HeroController.BindCompleted))]
        [HarmonyPostfix]
        private static void SetDuration(HeroController __instance)
        {
            if (_instance.IsActive && __instance.reaperState.IsInReaperMode)
            {
                __instance.reaperState.ReaperModeDurationLeft = _instance.Seconds.Value;
                DebugLog.Tweak(_instance, $"Silk Steal lasts {_instance.Seconds.Value} s");
            }
        }
    }
}
