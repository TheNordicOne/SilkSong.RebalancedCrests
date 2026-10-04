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
            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Hunter, 0.1667f, "Attack Antic"));
            _tweaks.Add(new NeedleStrikePushbackTweak(Config));
            _tweaks.Add(new FocusLossTweak(Config));

            _tweaks.Add(new UpSlashInFrontTweak(Config));
            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Reaper, 0.125f, "Reaper Antic"));
            _tweaks.Add(new NeedleStrikeRangeTweak(Config));
            _tweaks.Add(new DownSlashTweak(Config));
            _tweaks.Add(new FixedSilkBundlesTweak(Config));
            _tweaks.Add(new NeedleStrikeSilkBundlesTweak(Config));
            _tweaks.Add(new SilkSteelDurationTweak(Config));

            _tweaks.Add(new SprintSlashWindupTweak(Config, Crest.Wanderer, 0.1111f, "Wanderer Nml", "Wanderer Alt"));

            CrestDump.Init(Config, Logger);
            HeroEvents.HeroStarted += ApplyTweaks;
            Config.SettingChanged += (_, _) => ApplyTweaks(HeroController.UnsafeInstance);
            new Harmony(Info.Metadata.GUID).PatchAll(typeof(RebalancedCrests).Assembly);
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
