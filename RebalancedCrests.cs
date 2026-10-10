using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using RebalancedCrests.Core;
using RebalancedCrests.Crests;
using RebalancedCrests.Crests.Hunter;
using RebalancedCrests.Crests.Reaper;
using RebalancedCrests.Crests.Shared;
using RebalancedCrests.Diagnostics;

namespace RebalancedCrests
{
    [BepInPlugin("com.thenordicone.rebalancedcrests", "Rebalanced Crests", "0.1.0")]
    public class RebalancedCrests : BaseUnityPlugin
    {
        private readonly List<Tweak> _tweaks = new List<Tweak>();

        private void Awake()
        {
            foreach (var crest in Crest.All)
            {
                _tweaks.Add(new MovementAfterAttackTweak(Config, crest));
            }

            _tweaks.Add(new PogoWindupTweak(Config));
            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Hunter, 0.1667f, -1, "Attack Antic"));
            _tweaks.Add(new NeedleStrikePushbackTweak(Config));
            _tweaks.Add(new FocusLossTweak(Config));

            _tweaks.Add(new UpSlashInFrontTweak(Config));
            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Reaper, 0.125f, -1, "Reaper Antic"));
            _tweaks.Add(new NeedleStrikeRangeTweak(Config));
            _tweaks.Add(new DownSlashTweak(Config));
            _tweaks.Add(new FixedSilkBundlesTweak(Config));
            _tweaks.Add(new NeedleStrikeSilkBundlesTweak(Config));
            _tweaks.Add(new BindEffectDurationTweak(Config));
            _tweaks.Add(new HarvestBindTweak(Config));

            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Wanderer, 0.1111f, 2, "Wanderer Nml", "Wanderer Alt"));

            DebugLog.Init(Config, Logger);
            CrestDump.Init(Config, Logger);
            HeroEvents.HeroStarted += ApplyTweaks;
            Config.SettingChanged += (_, _) => ApplyTweaks(HeroController.UnsafeInstance);
            var harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll(typeof(RebalancedCrests).Assembly);
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched {method.DeclaringType?.Name}.{method.Name}");
            }
            Logger.LogInfo("loaded");
        }

        private void ApplyTweaks(HeroController hero)
        {
            if (hero == null)
            {
                return;
            }

            foreach (var tweak in _tweaks)
            {
                tweak.Apply(hero);
            }
        }
    }
}
