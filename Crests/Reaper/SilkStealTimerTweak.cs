using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Warns that Silk Steal (reaper mode) is about to end: during its last seconds the glow of the Reaper HUD frame
    /// (BindOrbHudFrame.reaperModeEffect) blinks, switching between full and <see cref="BlinkAlpha"/> every
    /// <see cref="BlinkInterval"/> seconds of ReaperModeDurationLeft. ReaperCrestRoutine sets the glow to full when
    /// reaper mode begins and fades it out when it ends; SilkStealBlinkDisplay sets the alpha in between.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealTimerTweak : Tweak
    {
        internal const float BlinkAlpha = 0.15f;
        internal const float BlinkInterval = 0.15f;

        internal SilkStealTimerTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Blink on HUD before ending",
                "The glow around the HUD frame blinks during the last seconds of Silk Steal. "
                + "Vanilla: the glow stays full until Silk Steal ends.")
        {
            BlinkSeconds = config.Bind(Crest.Name, "Silk Steal - Blink seconds", 3f,
                new ConfigDescription("How many seconds before Silk Steal ends the glow starts blinking. "
                    + "Silk Steal lasts 10 seconds.",
                    new AcceptableValueRange<float>(0.5f, 10f)));
            Instance = this;
        }

        internal static SilkStealTimerTweak Instance { get; private set; }

        internal ConfigEntry<float> BlinkSeconds { get; }

        [HarmonyPatch(typeof(BindOrbHudFrame), "Awake")]
        [HarmonyPostfix]
        private static void Attach(BindOrbHudFrame __instance)
        {
            __instance.gameObject.AddComponent<SilkStealBlinkDisplay>().Frame = __instance;
        }
    }

    internal sealed class SilkStealBlinkDisplay : MonoBehaviour
    {
        internal BindOrbHudFrame Frame { get; set; }

        private void LateUpdate()
        {
            var tweak = SilkStealTimerTweak.Instance;
            var hero = HeroController.instance;
            var glow = Frame.reaperModeEffect;
            if (tweak == null || !tweak.IsActive || hero == null || !glow || !hero.reaperState.IsInReaperMode
                || !Frame.isActive || Frame.currentFrameAnims != Frame.reaperFrameAnims)
            {
                return;
            }

            var timeLeft = hero.reaperState.ReaperModeDurationLeft;
            var blinkOff = timeLeft <= tweak.BlinkSeconds.Value
                           && Mathf.FloorToInt(timeLeft / SilkStealTimerTweak.BlinkInterval) % 2 == 1;
            glow.AlphaSelf = blinkOff ? SilkStealTimerTweak.BlinkAlpha : 1f;
        }
    }
}
