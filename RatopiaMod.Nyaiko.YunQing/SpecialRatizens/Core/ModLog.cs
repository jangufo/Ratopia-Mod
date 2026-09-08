using BepInEx.Logging;

namespace SpecialRatizens.Core
{
    /// <summary>
    /// 统一日志出口（对齐 RatopiaMod.YunQing.All 的 ModLog 风格）：
    /// Info 记录关键生命周期事件（启动、注册、读档、招募、大事件），
    /// Debug 记录高频运行时细节（默认日志级别下不显示），
    /// Warn/Error 记录配置与运行异常。
    /// </summary>
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
