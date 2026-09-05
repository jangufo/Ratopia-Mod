using System;
using System.Collections.Generic;
using System.Linq;
using RatopiaMod.YunQing.All.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RatopiaMod.YunQing.All.Localization
{
    internal sealed class CheatPanelTranslator
    {
        private readonly TranslationFileStore _store;
        private readonly List<Tuple<string, string>> _translations = [];
        private Font _chineseFont;

        public CheatPanelTranslator(TranslationFileStore store)
        {
            _store = store;
        }

        public void Localize(CheatMgr cheat)
        {
            if (cheat == null || cheat.Obj_Main == null)
            {
                ModLog.Warn("调试控制台或其主对象未初始化，跳过汉化。");
                return;
            }

            if (!_store.Exists)
            {
                ExportCheatPanelKeys(cheat);
                return;
            }

            EnsureChineseFont();
            _translations.Clear();
            _translations.AddRange(_store.Load());
            ApplyTranslations();
        }

        private void ExportCheatPanelKeys(CheatMgr cheat)
        {
            _translations.Clear();
            foreach (var page in cheat.Obj_Main)
            {
                if (page == null)
                {
                    continue;
                }

                AddTextKeys(page.GetComponentsInChildren<Text>(true));
                AddTextKeys(page.GetComponentsInChildren<TMP_Text>(true));
            }

            ModLog.Info($"未找到翻译文件，已导出 {_translations.Count} 个控制台文本键。");
            _store.Save(_translations);
        }

        private void AddTextKeys<T>(T[] texts) where T : Component
        {
            foreach (var text in texts)
            {
                AddKey(text.transform);
            }
        }

        private void AddKey(Transform transform)
        {
            var key = GetFullPath(transform);
            if (_translations.Any(translation => translation.Item1 == key))
            {
                return;
            }

            _translations.Add(new Tuple<string, string>(key, string.Empty));
        }

        private void ApplyTranslations()
        {
            var translatedCount = 0;
            foreach (var translation in _translations)
            {
                if (string.IsNullOrEmpty(translation.Item2))
                {
                    continue;
                }

                if (TranslateObject(translation.Item1, translation.Item2))
                {
                    translatedCount++;
                }
            }

            ModLog.Info($"调试控制台汉化完成，成功处理 {translatedCount} 条翻译。");
        }

        private bool TranslateObject(string path, string value)
        {
            var gameObject = GameObject.Find(path);
            if (gameObject == null)
            {
                ModLog.Debug($"翻译目标不存在或未激活：{path}");
                return false;
            }

            var text = gameObject.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
                text.fontSize = 14;
                if (_chineseFont != null)
                {
                    text.font = _chineseFont;
                }

                return true;
            }

            var tmpText = gameObject.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = value;
                tmpText.fontSize = 14;
                return true;
            }

            ModLog.Debug($"翻译目标没有可用的文本组件：{path}");
            return false;
        }

        private void EnsureChineseFont()
        {
            if (_chineseFont != null)
            {
                return;
            }

            var fonts = Resources.LoadAll<Font>(string.Empty);
            if (fonts.Length == 0)
            {
                ModLog.Warn("未找到可用于控制台汉化的字体。");
                return;
            }

            _chineseFont = fonts[0];
            ModLog.Debug($"控制台汉化字体已加载：{_chineseFont.name}");
        }

        private static string GetFullPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            if (transform.parent == null)
            {
                return transform.name;
            }

            return GetFullPath(transform.parent) + "/" + transform.name;
        }
    }
}
