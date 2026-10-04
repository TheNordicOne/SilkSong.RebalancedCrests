using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using RebalancedCrests.Core;
using RebalancedCrests.Crests.Hunter;
using RebalancedCrests.Diagnostics;

namespace RebalancedCrests
{
    [BepInPlugin("com.thenordicone.rebalancedcrests", "Rebalanced Crests", "0.1.0")]
    public class RebalancedCrests : BaseUnityPlugin
    {
        private readonly List<Tweak> _tweaks = new List<Tweak>();

        private void Awake()
        {
            _tweaks.Add(new PogoWindupTweak(Config));
            _tweaks.Add(new SprintSlashWindupTweak(Config));
            _tweaks.Add(new NeedleStrikePushbackTweak(Config));
            _tweaks.Add(new FocusLossTweak(Config));

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
