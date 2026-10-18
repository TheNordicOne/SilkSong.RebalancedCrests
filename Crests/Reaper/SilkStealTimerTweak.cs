using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Shows the time left of Silk Steal (reaper mode) on the HUD: the glow of the Reaper HUD frame
    /// (BindOrbHudFrame.reaperModeEffect) fades from full to <see cref="MinAlpha"/> as
    /// ReaperModeDurationLeft runs down from Gameplay.ReaperModeDuration. ReaperCrestRoutine sets the glow to full
    /// when reaper mode begins and fades it out from its current alpha when it ends; in between, SilkStealTimerDisplay
    /// sets the alpha every frame.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealTimerTweak : Tweak
    {
        internal const float MinAlpha = 0.15f;

        internal SilkStealTimerTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Timer on HUD",
                "The glow around the HUD frame during Silk Steal fades as its time runs out. "
                + "Vanilla: the glow stays full until Silk Steal ends.")
        {
            Instance = this;
        }

        internal static SilkStealTimerTweak Instance { get; private set; }

        [HarmonyPatch(typeof(BindOrbHudFrame), "Awake")]
        [HarmonyPostfix]
        private static void Attach(BindOrbHudFrame __instance)
        {
            __instance.gameObject.AddComponent<SilkStealTimerDisplay>().Frame = __instance;
        }
    }

    internal sealed class SilkStealTimerDisplay : MonoBehaviour
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

            var timeLeft = Mathf.Clamp01(hero.reaperState.ReaperModeDurationLeft / Gameplay.ReaperModeDuration);
            glow.AlphaSelf = Mathf.Lerp(SilkStealTimerTweak.MinAlpha, 1f, timeLeft);
        }
    }
}
