using System.Collections.Generic;
using BepInEx.Configuration;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Warns that Silk Steal (reaper mode) is about to end: during its last seconds the glow around Hornet
    /// (HeroController.reaperModeEffect, "crest_reaper_effects(Clone)") pulses, fading out and in once per
    /// 2 × 'Blink speed' seconds of ReaperModeDurationLeft. Its sprites ("glow", "reaper_halo_loop",
    /// "reaper_halo_burst") are SpriteRenderers. Their alpha is scaled in LateUpdate, after the effect's Animator, and
    /// restored when the pulse ends. The "Fury_ring" particles are not faded.
    /// </summary>
    internal sealed class SilkStealTimerTweak : Tweak
    {
        internal const float MinAlpha = 0.1f;

        internal SilkStealTimerTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Silk Steal - Blink on Hornet before ending",
                "The glow around Hornet fades in and out during the last seconds of Silk Steal. "
                + "Vanilla: the glow stays until Silk Steal ends.")
        {
            BlinkSeconds = config.Bind(Crest.Name, "Silk Steal - Blink seconds", 3f,
                new ConfigDescription("How many seconds before Silk Steal ends the glow starts blinking.",
                    new AcceptableValueRange<float>(0.5f, 10f)));
            BlinkInterval = config.Bind(Crest.Name, "Silk Steal - Blink speed", 0.15f,
                new ConfigDescription("Seconds the glow takes to fade out, and then in, per blink. Lower is faster.",
                    new AcceptableValueRange<float>(0.05f, 1f)));
            Instance = this;
        }

        internal static SilkStealTimerTweak Instance { get; private set; }

        internal ConfigEntry<float> BlinkSeconds { get; }

        internal ConfigEntry<float> BlinkInterval { get; }

        internal override void Apply(HeroController hero)
        {
            if (!hero.GetComponent<SilkStealBlinkDisplay>())
            {
                hero.gameObject.AddComponent<SilkStealBlinkDisplay>();
            }
        }
    }

    internal sealed class SilkStealBlinkDisplay : MonoBehaviour
    {
        private readonly Dictionary<SpriteRenderer, float> _baseAlpha = new Dictionary<SpriteRenderer, float>();
        private HeroController _hero;

        private void Awake()
        {
            _hero = GetComponent<HeroController>();
        }

        private void LateUpdate()
        {
            var tweak = SilkStealTimerTweak.Instance;
            var glow = _hero.reaperModeEffect;
            var state = _hero.reaperState;
            if (tweak == null || !tweak.IsActive || !glow || !state.IsInReaperMode
                || state.ReaperModeDurationLeft > tweak.BlinkSeconds.Value)
            {
                Restore();
                return;
            }

            // 1 at the start of each blink, MinAlpha halfway through it
            var phase = state.ReaperModeDurationLeft / tweak.BlinkInterval.Value * Mathf.PI;
            var factor = Mathf.Lerp(SilkStealTimerTweak.MinAlpha, 1f, 0.5f + 0.5f * Mathf.Cos(phase));
            foreach (var sprite in glow.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!_baseAlpha.TryGetValue(sprite, out var baseAlpha))
                {
                    baseAlpha = sprite.color.a;
                    _baseAlpha[sprite] = baseAlpha;
                }

                var color = sprite.color;
                color.a = baseAlpha * factor;
                sprite.color = color;
            }
        }

        private void Restore()
        {
            if (_baseAlpha.Count == 0)
            {
                return;
            }

            foreach (var entry in _baseAlpha)
            {
                if (entry.Key)
                {
                    var color = entry.Key.color;
                    color.a = entry.Value;
                    entry.Key.color = color;
                }
            }

            _baseAlpha.Clear();
        }
    }
}
