using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RatopiaMod.YunQing.All.Logging;
using Formatting = Newtonsoft.Json.Formatting;

namespace RatopiaMod.YunQing.All.Localization
{
    internal sealed class TranslationFileStore
    {
        private readonly string _path;

        public TranslationFileStore(string path)
        {
            _path = path;
            ModLog.Debug($"翻译文件路径：{_path}");
        }

        public bool Exists => File.Exists(_path);

        public List<Tuple<string, string>> Load()
        {
            try
            {
                var translations = JsonConvert.DeserializeObject<List<Tuple<string, string>>>(File.ReadAllText(_path)) ??
                                   [];
                ModLog.Info($"翻译文件读取完成，共 {translations.Count} 条记录。");
                return translations;
            }
            catch (Exception exception)
            {
                ModLog.Error($"读取翻译文件失败：{_path}，原因：{exception.Message}");
                return [];
            }
        }

        public void Save(IEnumerable<Tuple<string, string>> translations)
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var records = translations.ToList();
                File.WriteAllText(_path, JsonConvert.SerializeObject(records, Formatting.Indented));
                ModLog.Info($"翻译文件保存完成，共 {records.Count} 条记录：{_path}");
            }
            catch (Exception exception)
            {
                ModLog.Error($"保存翻译文件失败：{_path}，原因：{exception.Message}");
            }
        }
    }
}
