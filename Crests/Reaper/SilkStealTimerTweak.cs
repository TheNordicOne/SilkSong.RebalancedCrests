using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Warns that Silk Steal (reaper mode) is about to end: during its last seconds the glow of the Reaper HUD frame
    /// (BindOrbHudFrame.reaperModeEffect, "crest_reaper_effects") blinks, hidden and shown every
    /// 'Blink speed' seconds of ReaperModeDurationLeft. The NestedFadeGroup alpha does not reach its
    /// visible renderer ("reaper_halo_loop"), so SilkStealBlinkDisplay disables its enabled renderers instead and
    /// re-enables them afterwards.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealTimerTweak : Tweak
    {
        internal SilkStealTimerTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Blink on HUD before ending",
                "The glow around the HUD frame blinks during the last seconds of Silk Steal. "
                + "Vanilla: the glow stays full until Silk Steal ends.")
        {
            BlinkSeconds = config.Bind(Crest.Name, "Silk Steal - Blink seconds", 3f,
                new ConfigDescription("How many seconds before Silk Steal ends the glow starts blinking. "
                    + "Silk Steal lasts 10 seconds.",
                    new AcceptableValueRange<float>(0.5f, 10f)));
            BlinkInterval = config.Bind(Crest.Name, "Silk Steal - Blink speed", 0.5f,
                new ConfigDescription("Seconds the glow stays hidden, and then shown, per blink. Lower is faster.",
                    new AcceptableValueRange<float>(0.05f, 1f)));
            Instance = this;
        }

        internal static SilkStealTimerTweak Instance { get; private set; }

        internal ConfigEntry<float> BlinkSeconds { get; }

        internal ConfigEntry<float> BlinkInterval { get; }

        [HarmonyPatch(typeof(BindOrbHudFrame), "Awake")]
        [HarmonyPostfix]
        private static void Attach(BindOrbHudFrame __instance)
        {
            __instance.gameObject.AddComponent<SilkStealBlinkDisplay>().Frame = __instance;
        }
    }

    /// <summary>
    /// Blinks the glow around Hornet (HeroController.reaperModeEffect) like the HUD glow, with the blink settings of
    /// <see cref="SilkStealTimerTweak"/>.
    /// </summary>
    internal sealed class SilkStealHeroBlinkTweak : Tweak
    {
        internal SilkStealHeroBlinkTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Blink on Hornet before ending",
                "The glow around Hornet blinks during the last seconds of Silk Steal (uses 'Silk Steal - Blink seconds' "
                + "and 'Silk Steal - Blink speed'). Vanilla: the glow stays until Silk Steal ends.")
        {
            Instance = this;
        }

        internal static SilkStealHeroBlinkTweak Instance { get; private set; }
    }

    /// <summary>
    /// Hides the enabled renderers of a glow during the off phases of the blink and shows them again afterwards.
    /// </summary>
    internal sealed class SilkStealBlinkDisplay : MonoBehaviour
    {
        private readonly List<Renderer> _hiddenHud = new List<Renderer>();
        private readonly List<Renderer> _hiddenHero = new List<Renderer>();

        internal BindOrbHudFrame Frame { get; set; }

        private void LateUpdate()
        {
            var timing = SilkStealTimerTweak.Instance;
            var hero = HeroController.instance;
            var blinkOff = timing != null && hero != null && hero.reaperState.IsInReaperMode
                           && hero.reaperState.ReaperModeDurationLeft <= timing.BlinkSeconds.Value
                           && Mathf.FloorToInt(hero.reaperState.ReaperModeDurationLeft / timing.BlinkInterval.Value) % 2 == 1;

            var hudGlow = Frame.reaperModeEffect;
            SetHidden(_hiddenHud, hudGlow ? hudGlow.gameObject : null, blinkOff && timing.IsActive && Frame.isActive
                                                                       && Frame.currentFrameAnims == Frame.reaperFrameAnims);

            var heroGlow = hero != null && hero.reaperModeEffect ? hero.reaperModeEffect.gameObject : null;
            SetHidden(_hiddenHero, heroGlow, blinkOff && SilkStealHeroBlinkTweak.Instance?.IsActive == true);
        }

        private static void SetHidden(List<Renderer> hidden, GameObject glow, bool hide)
        {
            if (hide && glow)
            {
                foreach (var renderer in glow.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        hidden.Add(renderer);
                    }
                }
            }
            else if (!hide && hidden.Count > 0)
            {
                foreach (var renderer in hidden)
                {
                    if (renderer)
                    {
                        renderer.enabled = true;
                    }
                }

                hidden.Clear();
            }
        }
    }
}
