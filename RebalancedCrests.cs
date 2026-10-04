using BepInEx;

namespace RebalancedCrests
{
    [BepInPlugin("com.thenordicone.rebalancedcrests", "Rebalanced Crests", "0.1.0")]
    public class RebalancedCrests : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("loaded");
        }
    }
}
