using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using RebalancedCrests.Core;
using TeamCherry.NestedFadeGroup;
using TeamCherry.SharedUtils;
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

    /// <summary>
    /// Shows the time left of Silk Steal on Hornet: her glow (HeroController.reaperModeEffect, a
    /// NestedFadeGroupTimedFader with manual trigger) fades like the HUD glow. When Silk Steal ends, the fader's Fade()
    /// sets the alpha back to its start value (1) before fading out over 0.2 s; StartFade starts from the current
    /// alpha instead, so the faded glow does not flash.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealHeroTimerTweak : Tweak
    {
        internal SilkStealHeroTimerTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Timer on Hornet",
                "The glow around Hornet during Silk Steal fades as its time runs out. "
                + "Vanilla: the glow stays full until Silk Steal ends.")
        {
            Instance = this;
        }

        internal static SilkStealHeroTimerTweak Instance { get; private set; }

        [HarmonyPatch(typeof(NestedFadeGroupTimedFader), "StartFade")]
        [HarmonyPrefix]
        private static void FadeFromCurrent(NestedFadeGroupTimedFader __instance, out MinMaxFloat? __state)
        {
            __state = null;
            var hero = HeroController.instance;
            if (Instance == null || !Instance.IsActive || hero == null || __instance != hero.reaperModeEffect)
            {
                return;
            }

            __state = __instance.fadeAlpha;
            __instance.fadeAlpha.Start = __instance.fade.AlphaSelf;
        }

        [HarmonyPatch(typeof(NestedFadeGroupTimedFader), "StartFade")]
        [HarmonyPostfix]
        private static void RestoreFadeStart(NestedFadeGroupTimedFader __instance, MinMaxFloat? __state)
        {
            if (__state.HasValue)
            {
                __instance.fadeAlpha = __state.Value;
            }
        }
    }

    /// <summary>
    /// Sets both glows every frame while Silk Steal lasts, after their own updates.
    /// </summary>
    internal sealed class SilkStealTimerDisplay : MonoBehaviour
    {
        internal BindOrbHudFrame Frame { get; set; }

        private void LateUpdate()
        {
            var hero = HeroController.instance;
            if (hero == null || !hero.reaperState.IsInReaperMode)
            {
                return;
            }

            var timeLeft = Mathf.Clamp01(hero.reaperState.ReaperModeDurationLeft / Gameplay.ReaperModeDuration);
            var alpha = Mathf.Lerp(SilkStealTimerTweak.MinAlpha, 1f, timeLeft);

            var hudGlow = Frame.reaperModeEffect;
            if (SilkStealTimerTweak.Instance?.IsActive == true && hudGlow && Frame.isActive
                && Frame.currentFrameAnims == Frame.reaperFrameAnims)
            {
                hudGlow.AlphaSelf = alpha;
            }

            var heroGlow = hero.reaperModeEffect;
            if (SilkStealHeroTimerTweak.Instance?.IsActive == true && heroGlow && heroGlow.isActiveAndEnabled)
            {
                heroGlow.fade.AlphaSelf = alpha;
            }
        }
    }
}
