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
    /// <see cref="BlinkInterval"/> seconds of ReaperModeDurationLeft. The NestedFadeGroup alpha does not reach its
    /// visible renderer ("reaper_halo_loop"), so SilkStealBlinkDisplay disables its enabled renderers instead and
    /// re-enables them afterwards.
    /// </summary>
    [HarmonyPatch]
    internal sealed class SilkStealTimerTweak : Tweak
    {
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
        private readonly List<Renderer> _hidden = new List<Renderer>();

        internal BindOrbHudFrame Frame { get; set; }

        private void LateUpdate()
        {
            var tweak = SilkStealTimerTweak.Instance;
            var hero = HeroController.instance;
            var glow = Frame.reaperModeEffect;
            var blinkOff = tweak != null && tweak.IsActive && hero != null && glow && hero.reaperState.IsInReaperMode
                           && Frame.isActive && Frame.currentFrameAnims == Frame.reaperFrameAnims
                           && hero.reaperState.ReaperModeDurationLeft <= tweak.BlinkSeconds.Value
                           && Mathf.FloorToInt(hero.reaperState.ReaperModeDurationLeft / SilkStealTimerTweak.BlinkInterval) % 2 == 1;
            if (blinkOff)
            {
                foreach (var renderer in glow.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        _hidden.Add(renderer);
                    }
                }
            }
            else if (_hidden.Count > 0)
            {
                foreach (var renderer in _hidden)
                {
                    if (renderer)
                    {
                        renderer.enabled = true;
                    }
                }

                _hidden.Clear();
            }
        }
    }
}
