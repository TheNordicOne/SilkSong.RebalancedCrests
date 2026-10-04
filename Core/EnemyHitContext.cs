using HarmonyLib;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Exposes the hit currently being processed by HealthManager.TakeDamage to code called from inside it.
    /// </summary>
    [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
    internal static class EnemyHitContext
    {
        internal static HitInstance? Current { get; private set; }

        private static void Prefix(HitInstance hitInstance, out HitInstance? __state)
        {
            __state = Current;
            Current = hitInstance;
        }

        private static System.Exception Finalizer(HitInstance? __state, System.Exception __exception)
        {
            Current = __state;
            return __exception;
        }
    }
}
