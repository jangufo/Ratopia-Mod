using System;
using System.IO;
using SpecialRatizens.Core;
using UnityEngine;

namespace RatopiaMod
{
    internal class BaseCommand
    {
        public static TextureWrapMode TexWrapMode = TextureWrapMode.Clamp;

        public static FilterMode FilMode = FilterMode.Bilinear;

        /// <summary>
        /// 加载Sprite
        /// </summary>
        /// <param name="path"></param>
        /// <param name="customPath"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(string path, string customPath = null)
        {
            Sprite sprite = null;

            //优先从Resources中加载
            sprite = Resources.Load<Sprite>(path);

            if (sprite != null)
                return sprite;

            sprite = LoadSpriteFromTexture2D(LoadTextureFromFile(customPath ?? path));

            ModLog.Debug($"从自定义路径 {path} 加载了图片 {sprite}");

            if (sprite == null)
                sprite = Resources.Load<Sprite>("Missing");

            return sprite;
        }

        /// <summary>
        /// 从Texture2D中加载Sprite
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static Sprite LoadSpriteFromTexture2D(Texture2D tex)
        {
            return tex == null ? null : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 加载Texture2D图片文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static Texture2D LoadTextureFromFile(string path, string suffix = ".png")
        {
            string fileName = path.IndexOf(suffix) >= 0 ? path : $"{path}{suffix}";

            byte[] bytes = LoadFile(fileName);

            if (bytes != null)
            {
                var tex = new Texture2D(2048, 2048, TextureFormat.RGBA32, false)
                {
                    wrapMode = TexWrapMode,

                    filterMode = FilMode
                };

                tex.LoadImage(bytes, false);

                int index = fileName.LastIndexOf('/');

                string fullName = fileName.Substring(index != -1 ? index + 1 : 0);

                tex.name = FileNameWithoutExtensions(fullName);

                return tex;
            }
            else
            {
                ModLog.Warn($"load non byte from {fileName}");

                return null;
            }
        }

        /// <summary>
        /// 获取文件名（去后缀名）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static string FileNameWithoutExtensions(string fileName)
        {
            if (fileName.LastIndexOf(".") == -1)
                return fileName;

            return fileName.Substring(0, fileName.LastIndexOf("."));
        }

        /// <summary>
        /// 加载文件
        /// </summary>
        /// <param name="path">路径（包含后缀名）</param>
        /// <returns></returns>
        public static byte[] LoadFile(string path)
        {
            try
            {
                byte[] bytes = null;

                if (File.Exists(path))
                {
                    bytes = File.ReadAllBytes(path);
                }

                return bytes;
            }
            catch (Exception ex)
            {
                ModLog.Warn(ex.ToString());

                return null;
            }
        }

        /// <summary>
        /// 字符串转枚举
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T StringToEnum<T>(string str, bool firstCharUpper = false)
        {
            TryParseStringToEnum(str, out T result, firstCharUpper);

            return result;
        }

        /// <summary>
        /// 尝试转换string为enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <param name="firstCharUpper"></param>
        /// <returns></returns>
        public static bool TryParseStringToEnum<T>(string str, out T result, bool firstCharUpper = false)
        {
            if (firstCharUpper)
                str = FirstCharToUpper(str);

            try
            {
                result = (T)Enum.Parse(typeof(T), str);
            }
            catch
            {
                result = default;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FirstCharToUpper(string str)
        {
            if (str == null || str.Equals(""))
                return str;

            return $"{str.Substring(0, 1).ToUpper()}{str.Substring(1)}";
        }
    }
}
