using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatopiaMod;

namespace SpecialRatizens.Core
{
    /// <summary>
    /// 特殊鼠鼠数据目录：从 Data 目录加载 JSON 数据文件（Newtonsoft.Json），
    /// 全量校验后同时提供校验视图（Ratizens/Traits）与游戏运行时对象
    /// （RuntimeTraits: CharacterInfo，RuntimeRatizens: CustomSpecialUnit）。
    /// 任一校验失败都会抛出 InvalidDataException，由插件入口整体拒绝启用。
    /// </summary>
    internal sealed class SpecialDataCatalog
    {
        public const string RatizenFileName = "CustomSpecialUnit.json";
        public const string TraitFileName = "CustomCharInfo.json";
        public const string IconFolderName = "Icon";

        private static readonly string[] RequiredTraitFields =
        {
            "Category", "Name", "T_Name", "EffectValue_A", "EffectValue_B", "Description"
        };

        private static readonly string[] RequiredRatizenFields =
        {
            "name", "nameColor", "LockStatus", "UnitGender", "grade", "pow", "dex", "wit", "gold",
            "char1", "icon1", "char2", "icon2", "probability", "skin", "face", "bread", "dress",
            "glasses", "hair", "hat", "makeup"
        };

        private SpecialDataCatalog(
            IReadOnlyList<SpecialRatizenDefinition> ratizens,
            IReadOnlyList<SpecialTraitDefinition> traits,
            IReadOnlyList<CharacterInfo> runtimeTraits,
            IReadOnlyList<CustomSpecialUnit> runtimeRatizens)
        {
            Ratizens = ratizens;
            Traits = traits;
            RuntimeTraits = runtimeTraits;
            RuntimeRatizens = runtimeRatizens;
        }

        /// <summary>校验通过后的特殊鼠鼠定义视图。</summary>
        public IReadOnlyList<SpecialRatizenDefinition> Ratizens { get; }

        /// <summary>校验通过后的特性定义视图。</summary>
        public IReadOnlyList<SpecialTraitDefinition> Traits { get; }

        /// <summary>校验通过后的游戏特性对象（可直接注册进游戏数据库）。</summary>
        public IReadOnlyList<CharacterInfo> RuntimeTraits { get; }

        /// <summary>校验通过后的特殊鼠鼠运行时对象（含抽取状态字段）。</summary>
        public IReadOnlyList<CustomSpecialUnit> RuntimeRatizens { get; }

        public static SpecialDataCatalog Load(string dataRoot)
        {
            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                throw new InvalidDataException("特殊鼠鼠数据目录不能为空。");
            }

            var ratizenPath = Path.Combine(dataRoot, RatizenFileName);
            var traitPath = Path.Combine(dataRoot, TraitFileName);
            var iconDirectory = Path.Combine(dataRoot, IconFolderName);

            RequireFile(ratizenPath, "特殊鼠鼠 JSON");
            RequireFile(traitPath, "特性 JSON");
            if (!Directory.Exists(iconDirectory))
            {
                throw new InvalidDataException($"图标目录不存在：{iconDirectory}");
            }

            try
            {
                var traitArray = ParseArray(File.ReadAllText(traitPath, Encoding.UTF8), "特性 JSON");
                var ratizenArray = ParseArray(File.ReadAllText(ratizenPath, Encoding.UTF8), "特殊鼠鼠 JSON");

                var runtimeTraits = ParseTraits(traitArray);
                var runtimeRatizens = ParseRatizens(ratizenArray, runtimeTraits, iconDirectory);

                return new SpecialDataCatalog(
                    new ReadOnlyCollection<SpecialRatizenDefinition>(BuildRatizenDefinitions(runtimeRatizens)),
                    new ReadOnlyCollection<SpecialTraitDefinition>(BuildTraitDefinitions(runtimeTraits)),
                    new ReadOnlyCollection<CharacterInfo>(runtimeTraits),
                    new ReadOnlyCollection<CustomSpecialUnit>(runtimeRatizens));
            }
            catch (JsonException error)
            {
                throw new InvalidDataException($"特殊鼠鼠 JSON 解析失败：{error.Message}", error);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception error) when (error is FormatException || error is OverflowException)
            {
                throw new InvalidDataException($"特殊鼠鼠数据格式错误：{error.Message}", error);
            }
        }

        private static JArray ParseArray(string text, string label)
        {
            var token = JToken.Parse(text);

            if (!(token is JArray array))
            {
                throw new InvalidDataException($"{label} 的根节点必须是数组。");
            }

            return array;
        }

        private static List<CharacterInfo> ParseTraits(JArray array)
        {
            var result = new List<CharacterInfo>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < array.Count; index++)
            {
                if (!(array[index] is JObject item))
                {
                    throw new InvalidDataException($"特性 JSON 第 {index + 1} 项必须是对象。");
                }

                RequireFields(item, RequiredTraitFields, "特性 JSON", index);

                var name = RequiredString(item, "Name", index);
                if (!names.Add(name))
                {
                    throw new InvalidDataException($"特性名称重复：{name}");
                }

                var category = RequiredInt(item, "Category", name);
                if (category != 0 && category != 1)
                {
                    throw new InvalidDataException($"特性 {name} 的 Category 必须为 0 或 1。");
                }

                var displayName = RequiredString(item, "T_Name", index);
                var valueA = RequiredFloat(item, "EffectValue_A", name);
                var valueB = RequiredFloat(item, "EffectValue_B", name);
                var description = RequiredString(item, "Description", index);

                var info = ToRuntime<CharacterInfo>(item, $"特性 {name}");
                info.Category = category;
                info.Name = name;
                info.T_Name = displayName;
                info.EffectValue_A = valueA;
                info.EffectValue_B = valueB;
                info.Description = description;

                result.Add(info);
            }

            if (result.Count == 0)
            {
                throw new InvalidDataException("特性 JSON 没有数据。");
            }

            return result;
        }

        private static List<CustomSpecialUnit> ParseRatizens(
            JArray array,
            IReadOnlyList<CharacterInfo> traits,
            string iconDirectory)
        {
            var result = new List<CustomSpecialUnit>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            var traitNames = new HashSet<string>(traits.Select(item => item.Name), StringComparer.Ordinal);
            var traitOwners = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < array.Count; index++)
            {
                if (!(array[index] is JObject item))
                {
                    throw new InvalidDataException($"特殊鼠鼠 JSON 第 {index + 1} 项必须是对象。");
                }

                RequireFields(item, RequiredRatizenFields, "特殊鼠鼠 JSON", index);

                var name = RequiredString(item, "name", index);
                if (!names.Add(name))
                {
                    throw new InvalidDataException($"特殊鼠鼠名称重复：{name}");
                }

                var lockStatus = RequiredString(item, "LockStatus", name);
                if (lockStatus != "Unlock" && lockStatus != "Lock")
                {
                    throw new InvalidDataException($"特殊鼠鼠 {name} 的 LockStatus 无效：{lockStatus}");
                }

                var gender = RequiredString(item, "UnitGender", name);
                if (gender != "Male" && gender != "Female")
                {
                    throw new InvalidDataException($"特殊鼠鼠 {name} 的性别无效：{gender}");
                }

                var probability = RequiredInt(item, "probability", name);
                if (probability < 0 || probability > 10000)
                {
                    throw new InvalidDataException($"特殊鼠鼠 {name} 的概率必须在 0 到 10000 之间。");
                }

                var trait1 = RequiredString(item, "char1", name);
                var trait2 = RequiredString(item, "char2", name);
                if (!traitNames.Contains(trait1) || !traitNames.Contains(trait2))
                {
                    throw new InvalidDataException($"特殊鼠鼠 {name} 引用了不存在的特性：{trait1}/{trait2}");
                }

                if (lockStatus == "Unlock")
                {
                    AddTraitOwner(traitOwners, trait1, name);
                    AddTraitOwner(traitOwners, trait2, name);
                }

                var icon1 = RequiredString(item, "icon1", name);
                var icon2 = RequiredString(item, "icon2", name);
                RequireIcon(iconDirectory, name, icon1);
                RequireIcon(iconDirectory, name, icon2);

                RequiredString(item, "nameColor", name);
                RequiredInt(item, "grade", name);
                RequiredInt(item, "pow", name);
                RequiredInt(item, "dex", name);
                RequiredInt(item, "wit", name);
                RequiredInt(item, "gold", name);

                var unit = ToRuntime<CustomSpecialUnit>(item, $"特殊鼠鼠 {name}");
                unit.name = name;
                unit.nameColor = TextOrNull(item, "nameColor");
                unit.char1 = trait1;
                unit.icon1 = icon1;
                unit.char2 = trait2;
                unit.icon2 = icon2;
                unit.probability = probability;
                unit.skin = TextOrNull(item, "skin");
                unit.face = TextOrNull(item, "face");
                unit.bread = TextOrNull(item, "bread");
                unit.dress = TextOrNull(item, "dress");
                unit.glasses = TextOrNull(item, "glasses");
                unit.hair = TextOrNull(item, "hair");
                unit.hat = TextOrNull(item, "hat");
                unit.makeup = TextOrNull(item, "makeup");

                result.Add(unit);
            }

            if (result.Count == 0)
            {
                throw new InvalidDataException("特殊鼠鼠 JSON 没有数据。");
            }

            return result;
        }

        private static T ToRuntime<T>(JObject item, string owner)
        {
            try
            {
                return item.ToObject<T>();
            }
            catch (Exception error)
            {
                throw new InvalidDataException($"{owner} 无法映射到运行时对象：{error.Message}", error);
            }
        }

        private static List<SpecialTraitDefinition> BuildTraitDefinitions(IReadOnlyList<CharacterInfo> traits)
        {
            return traits
                .Select(item => new SpecialTraitDefinition(
                    item.Category, item.Name, item.T_Name, item.EffectValue_A, item.EffectValue_B, item.Description))
                .ToList();
        }

        private static List<SpecialRatizenDefinition> BuildRatizenDefinitions(IReadOnlyList<CustomSpecialUnit> ratizens)
        {
            return ratizens
                .Select(item => new SpecialRatizenDefinition(
                    item.name, item.nameColor, item.LockStatus, item.UnitGender, item.grade,
                    item.pow, item.dex, item.wit, item.gold,
                    item.char1, item.icon1, item.char2, item.icon2, item.probability,
                    item.skin, item.face, item.bread, item.dress, item.glasses, item.hair, item.hat, item.makeup))
                .ToList();
        }

        private static void RequireFields(JObject item, IEnumerable<string> fields, string label, int index)
        {
            foreach (var field in fields)
            {
                var value = item[field];

                if (value == null || value.Type == JTokenType.Null)
                {
                    throw new InvalidDataException($"{label} 第 {index + 1} 项缺少字段：{field}");
                }
            }
        }

        private static void AddTraitOwner(IDictionary<string, string> owners, string traitName, string ratizenName)
        {
            if (owners.TryGetValue(traitName, out var existingOwner))
            {
                throw new InvalidDataException(
                    $"特性 {traitName} 被多个特殊鼠鼠重复引用：{existingOwner}/{ratizenName}");
            }

            owners.Add(traitName, ratizenName);
        }

        private static string RequiredString(JObject item, string field, object owner)
        {
            var text = TextOrNull(item, field);

            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidDataException($"{owner} 的字段 {field} 不能为空。");
            }

            return text;
        }

        private static string TextOrNull(JObject item, string field)
        {
            var value = item[field];

            if (value == null || value.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            return value.ToString().Trim();
        }

        private static int RequiredInt(JObject item, string field, string owner)
        {
            var value = item[field];

            if (value == null || !(value is JValue jValue))
            {
                throw new InvalidDataException($"{owner} 的字段 {field} 不能为空。");
            }

            if (jValue.Type == JTokenType.Integer)
            {
                return (int)jValue;
            }

            if (jValue.Type == JTokenType.Float)
            {
                var number = (double)jValue;

                if (number != Math.Floor(number))
                {
                    throw new InvalidDataException($"{owner} 的字段 {field} 不是有效整数：{jValue}");
                }

                return (int)number;
            }

            throw new InvalidDataException($"{owner} 的字段 {field} 不是有效整数：{jValue}");
        }

        private static float RequiredFloat(JObject item, string field, string owner)
        {
            var value = item[field];

            if (value == null || !(value is JValue jValue))
            {
                throw new InvalidDataException($"{owner} 的字段 {field} 不能为空。");
            }

            if (jValue.Type == JTokenType.Integer || jValue.Type == JTokenType.Float)
            {
                return (float)(double)jValue;
            }

            throw new InvalidDataException($"{owner} 的字段 {field} 不是有效数值：{jValue}");
        }

        private static void RequireIcon(string iconDirectory, string ratizenName, string iconName)
        {
            var path = Path.Combine(iconDirectory, iconName + ".png");
            if (!File.Exists(path))
            {
                throw new InvalidDataException($"特殊鼠鼠 {ratizenName} 缺少图标：{path}");
            }
        }

        private static void RequireFile(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new InvalidDataException($"{label} 不存在：{path}");
            }
        }
    }
}
