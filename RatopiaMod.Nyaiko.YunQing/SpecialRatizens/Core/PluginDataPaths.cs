using System.IO;
using BepInEx;

namespace SpecialRatizens.Core
{
    /// <summary>
    /// 插件目录解析：统一使用 BepInEx 标准路径（Paths.PluginPath）与 System.IO 的 Path.Combine 拼接。
    /// 安装布局固定为 BepInEx/plugins/SpecialRatizens/，数据文件位于其下的 Data 目录。
    /// </summary>
    internal static class PluginDataPaths
    {
        public const string PluginFolderName = "SpecialRatizens";
        public const string DataFolderName = "Data";

        /// <summary>
        /// 插件根目录（SpecialRatizens.dll 与 Data 目录所在位置）。
        /// </summary>
        public static string ResolvePluginRoot()
        {
            return Path.Combine(Paths.PluginPath, PluginFolderName);
        }

        /// <summary>
        /// 数据目录（特殊鼠鼠、特性与图标 JSON/PNG 数据的根目录）。
        /// </summary>
        public static string ResolveDataRoot()
        {
            return Path.Combine(ResolvePluginRoot(), DataFolderName);
        }
    }
}
