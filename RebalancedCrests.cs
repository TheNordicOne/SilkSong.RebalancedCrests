using BepInEx;
using HarmonyLib;
using RebalancedCrests.Diagnostics;

namespace RebalancedCrests
{
    [BepInPlugin("com.thenordicone.rebalancedcrests", "Rebalanced Crests", "0.1.0")]
    public class RebalancedCrests : BaseUnityPlugin
    {
        private void Awake()
        {
            CrestDump.Init(Config, Logger);
            new Harmony(Info.Metadata.GUID).PatchAll(typeof(RebalancedCrests).Assembly);
            Logger.LogInfo("loaded");
        }
    }
}
