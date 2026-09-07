using BepInEx.Logging;

namespace RatopiaMod.YunQing.All.Logging
{
    internal static class ModLog
    {
        private static ManualLogSource _log;

        public static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        public static void Debug(string message)
        {
            _log?.LogDebug(message);
        }

        public static void Info(string message)
        {
            _log?.LogInfo(message);
        }

        public static void Warn(string message)
        {
            _log?.LogWarning(message);
        }

        public static void Error(string message)
        {
            _log?.LogError(message);
        }
    }
}
