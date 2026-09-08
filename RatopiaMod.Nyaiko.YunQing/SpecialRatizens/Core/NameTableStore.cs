using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace SpecialRatizens.Core
{
    /// <summary>
    /// 「更多名称」姓名表存储：从 Data/Names.json 惰性加载（首次访问才读文件），
    /// 文件缺失或损坏时降级为空表并记录警告，不影响特殊鼠鼠核心功能。
    /// </summary>
    internal sealed class NameTableStore
    {
        public const string FileName = "Names.json";

        private readonly Func<string> _dataRootAccessor;
        private bool _loaded;

        private string[] _surNames = Empty;
        private string[] _femaleOneChar = Empty;
        private string[] _femaleTwoChar = Empty;
        private string[] _maleOneChar = Empty;
        private string[] _maleTwoChar = Empty;

        private static readonly string[] Empty = new string[0];

        public NameTableStore(Func<string> dataRootAccessor)
        {
            _dataRootAccessor = dataRootAccessor ?? throw new ArgumentNullException(nameof(dataRootAccessor));
        }

        /// <summary>姓氏。</summary>
        public string[] SurNames { get { EnsureLoaded(); return _surNames; } }

        /// <summary>女名（单字）。</summary>
        public string[] FemaleOneChar { get { EnsureLoaded(); return _femaleOneChar; } }

        /// <summary>女名（双字）。</summary>
        public string[] FemaleTwoChar { get { EnsureLoaded(); return _femaleTwoChar; } }

        /// <summary>男名（单字）。</summary>
        public string[] MaleOneChar { get { EnsureLoaded(); return _maleOneChar; } }

        /// <summary>男名（双字）。</summary>
        public string[] MaleTwoChar { get { EnsureLoaded(); return _maleTwoChar; } }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            try
            {
                var path = Path.Combine(_dataRootAccessor(), FileName);

                if (!File.Exists(path))
                {
                    ModLog.Warn($"特殊鼠鼠姓名表不存在，相关功能将使用空表：{path}");
                    return;
                }

                var table = JsonConvert.DeserializeObject<NameTable>(File.ReadAllText(path, Encoding.UTF8));

                if (table == null)
                {
                    ModLog.Warn($"特殊鼠鼠姓名表为空：{path}");
                    return;
                }

                _surNames = table.SurNames ?? Empty;
                _femaleOneChar = table.FemaleOneChar ?? Empty;
                _femaleTwoChar = table.FemaleTwoChar ?? Empty;
                _maleOneChar = table.MaleOneChar ?? Empty;
                _maleTwoChar = table.MaleTwoChar ?? Empty;

                ModLog.Info(
                    $"特殊鼠鼠姓名表加载完成：姓氏 {_surNames.Length}，" +
                    $"女名 {_femaleOneChar.Length}/{_femaleTwoChar.Length}，" +
                    $"男名 {_maleOneChar.Length}/{_maleTwoChar.Length}");
            }
            catch (Exception error)
            {
                ModLog.Warn($"特殊鼠鼠姓名表加载失败，相关功能将使用空表：{error}");
            }
        }

        private sealed class NameTable
        {
            public string[] SurNames { get; set; }
            public string[] FemaleOneChar { get; set; }
            public string[] FemaleTwoChar { get; set; }
            public string[] MaleOneChar { get; set; }
            public string[] MaleTwoChar { get; set; }
        }
    }
}
