using System.IO;
using BepInEx;

namespace RatopiaMod.YunQing.All.Configuration
{
    internal static class PluginConstants
    {
        public const string Guid = "RatopiaMod.YunQing.YunQingAll";
        public const string Name = "RatopiaMod.YunQing.All";
        public const string Version = "3.0.0";

        public static string PluginDirectory => Path.Combine(Paths.PluginPath, Name);
        public static string TranslationFilePath => Path.Combine(PluginDirectory, "translate-kv.json");
    }
}
