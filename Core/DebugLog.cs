using BepInEx.Configuration;
using BepInEx.Logging;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Optional log of every moment a tweak changes what the game does, to verify tweaks while testing.
    /// </summary>
    internal static class DebugLog
    {
        private static ManualLogSource _log;
        private static ConfigEntry<bool> _enabled;

        internal static void Init(ConfigFile config, ManualLogSource log)
        {
            _log = log;
            _enabled = config.Bind("Debug", "Log tweaks", false,
                "Log every time a tweak changes what happens in game (BepInEx/LogOutput.log). For testing.");
        }

        internal static void Tweak(Tweak tweak, string message)
        {
            if (_enabled != null && _enabled.Value)
            {
                _log.LogInfo($"[{tweak.Crest.Name}] {message}");
            }
        }
    }
}
