using BepInEx;
using CasselGames.Audio;
using CasselGames.Data;
using CasselGames.Diplomatic;
using CasselGames.Diplomatic.Data;
using CasselGames.Diplomatic.UI;
using CasselGames.Encyclopedia;
using CasselGames.Input;
using CasselGames.UI;
using HarmonyLib;
using I2.Loc;
using Newtonsoft.Json;
using RatopiaMod;
using Spine;
using SpecialRatizens.Core;
using SpecialRatizens.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using Utility.Achievement;
using Utility.Data;
using Utility.IO;
using Utility.UI;
using static PathFindMgr;
using Random = UnityEngine.Random;

namespace RatopiaMod
{
    public class CustomMOD : BaseUnityPlugin
    {
        #region 名称表

        /// <summary>
        /// 「更多名称」姓名表：从 Data/Names.json 惰性加载（首次访问才读文件）。
        /// </summary>
        readonly static NameTableStore NameTables = new NameTableStore(() => CustomDataPath);

        /// <summary>
        /// 姓氏
        /// </summary>
        static string[] CustomSurNames { get { return NameTables.SurNames; } }

        #endregion

        #region 官方引用

        /// <summary>
        /// 游戏编辑管理器
        /// </summary>
        static EditorOptionMgr EditorMgr { get { return GameMgr.Instance._EditorMgr; } }
        /// <summary>
        /// 作弊管理器
        /// </summary>
        static CheatMgr CheatMgr { get { return DebugMgr.Instance._CheatMgr; } }
        /// <summary>
        /// 相机管理器
        /// </summary>
        static CameraMgr CameraMgr { get { return GameMgr.Instance._CamMgr; } }
        /// <summary>
        /// 系统管理器
        /// </summary>
        static SystemMgr SystemMgr { get { return GameMgr.Instance._SysMgr; } }
        /// <summary>
        /// 单位管理器
        /// </summary>
        static T_UnitMgr UnitMgr { get { return GameMgr.Instance._T_UnitMgr; } }
        /// <summary>
        /// 池管理器
        /// </summary>
        static PoolMgr PoolMgr { get { return GameMgr.Instance._PoolMgr; } }
        /// <summary>
        /// 统计管理器
        /// </summary>
        static StatisticsMgr SttMgr { get { return GameMgr.Instance._SttMgr; } }
        /// <summary>
        /// 数据管理器
        /// </summary>
        static DB_Mgr DBMgr { get { return GameMgr.Instance._DB_Mgr; } }
        /// <summary>
        /// 存储管理器
        /// </summary>
        static LoadMgr LoadMgr { get { return GameMgr.Instance._LoadMgr; } }
        static EconomicMgr EcoMgr { get { return GameMgr.Instance._EcoMgr; } }
        /// <summary>
        /// 外交管理器
        /// </summary>
        static DiplomaticMgr DiplomaticMgr { get { return GameMgr.Instance.DiplomaticMgr; } }
        /// <summary>
        /// 瓦片管理器
        /// </summary>
        static TileMgr TileMgr { get { return GameMgr.Instance._TileMgr; } }
        /// <summary>
        /// 建筑管理器
        /// </summary>
        static BuildingMgr BuildingMgr { get { return GameMgr.Instance._BuildingMgr; } }
        /// <summary>
        /// 天气管理器
        /// </summary>
        static WeatherMgr WeatherMgr { get { return GameMgr.Instance._WeatherMgr; } }
        /// <summary>
        /// 游戏数据管理器
        /// </summary>
        static PlayDataMgr PlayDataMgr { get { return PlayDataMgr.Instance; } }
        /// <summary>
        /// 绘图管理器
        /// </summary>
        static PallateMgr PallateMgr { get { return DebugMgr.Instance._PallateMgr; } }
        /// <summary>
        /// 寻路管理器
        /// </summary>
        static PathFindMgr PathFindMgr { get { return GameMgr.Instance._PathFindMgr; } }

        /// <summary>
        /// 繁荣界面
        /// </summary>
        static ProsperityUI ProsperityUI { get { return GameMgr.Instance._ProsperityUI; } }
        /// <summary>
        /// 法典界面
        /// </summary>
        static PolicyUI PolicyUI { get { return GameMgr.Instance._PolicyUI; } }
        /// <summary>
        /// 中间警告界面
        /// </summary>
        static CenterAlarmUI CenterAlarmUI { get { return GameMgr.Instance._CenterAlarmUI; } }
        /// <summary>
        /// 建筑界面
        /// </summary>
        static ConstructUI ConstructUI { get { return GameMgr.Instance._ConstructUI; } }
        /// <summary>
        /// 市民信息界面
        /// </summary>
        static CitizenInfoUI CitizenInfoUI { get { return GameMgr.Instance._CitizenInfoUI; } }
        /// <summary>
        /// 市民阶级信息界面
        /// </summary>
        static StatusCitizenInfoUI StatusCitizenInfoUI { get { return GetPrivateValue<StatusCitizenInfoUI>(CitizenInfoUI, "_statusCitizenInfoUI"); } }
        /// <summary>
        /// 移民界面
        /// </summary>
        static CitizenCaveUI CitizenCaveUI { get { return GameMgr.Instance._CCUI; } }

        /// <summary>
        /// 繁荣等级
        /// </summary>
        static int ProsperityLevel { get { return ProsperityUI.m_Level; } }
        /// <summary>
        /// 金币
        /// </summary>
        static float CountryGold { get { return EcoMgr.m_Gold; } set { EcoMgr.m_Gold = value; EcoMgr.m_GoldUI.TxtUpdate(); } }
        /// <summary>
        /// 外交数据
        /// </summary>
        static DiplomaticData DiplomaticData { get { return GetPrivateValue<DiplomaticData>(DiplomaticMgr, "_data"); } }
        /// <summary>
        /// 外交城市数据
        /// </summary>
        static Dictionary<Vector2Int, DiplomaticCountryData> CountryDatas { get { return GetPrivateValue<Dictionary<Vector2Int, DiplomaticCountryData>>(DiplomaticData, "_mapDic"); } }
        /// <summary>
        /// Sprite图片字典
        /// </summary>
        static Dictionary<string, Sprite> DicSprits { get { return GetPrivateValue<Dictionary<string, Sprite>>(Func.Instance, "Dic_Resource"); } }
        /// <summary>
        /// 所有居民
        /// </summary>
        static List<T_Citizen> Citizens { get { return UnitMgr.List_Citizen; } }
        /// <summary>
        /// 女王
        /// </summary>
        static T_Queen Queen { get { return UnitMgr.m_Queen; } }

        /// <summary>
        /// 游戏暂停中
        /// </summary>
        static bool GameIsPaused { get { return SystemMgr != null && SystemMgr.IsGamePause(); } }
        /// <summary>
        /// 当前游戏数据
        /// </summary>
        static D_Data GameData { get { return PlayDataMgr.Instance.m_GameData; } }

        #endregion

        /// <summary>
        /// 自定义数据路径
        /// </summary>
        static string CustomDataPath = string.Empty;

        /// <summary>
        /// 已校验的特殊鼠鼠数据目录（由插件入口在启动时加载并注入）。
        /// </summary>
        static SpecialDataCatalog CustomRatizenCatalog;

        /// <summary>
        /// 特殊鼠鼠功能总开关（BepInEx 配置，替代旧 GUI 设置）。
        /// </summary>
        static bool ActiveCustomSpecialUnit { get { return ModConfig.Instance != null && ModConfig.Instance.Enabled.Value; } }

        /// <summary>
        /// 「更多名称」随机姓名开关（BepInEx 配置）。
        /// </summary>
        static bool ActiveCustomNames { get { return ModConfig.Instance != null && ModConfig.Instance.CustomNames.Value; } }
        /// <summary>
        /// 由独立 BepInEx 5 入口配置。此兼容核心自身不会注册插件或自动安装补丁。
        /// </summary>
        internal static void ConfigureSpecialRatizens(string dataRoot, SpecialDataCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(dataRoot))
                throw new ArgumentException("特殊鼠鼠数据目录不能为空", nameof(dataRoot));

            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog), "特殊鼠鼠数据目录对象不能为空");

            CustomDataPath = Path.GetFullPath(dataRoot);
            CustomRatizenCatalog = catalog;
        }
        /// <summary>
        /// 读档完成后仅重建特殊鼠鼠会话状态与特性效果。
        /// </summary>
        public static void SpecialRatizensSessionLoaded()
        {
            ResetSpecialRatizensSession();
            LoadCitizenDatas();
            if (ActiveCustomSpecialUnit)
                UpdateAllUsedSpecialEffects();
        }

        /// <summary>
        /// 场景切换或插件卸载时清理所有非持久运行时引用。
        /// </summary>
        public static void ResetSpecialRatizensSession()
        {
            SpecialCitizens.Clear();
            SpecialCitizenSkins.Clear();
            specialUnit = null;
            foreach (CustomSpecialUnit unit in CustomSpecialUnitDatas.Values)
            {
                unit.isUsed = false;
                unit.pdr_C = 0;
            }
            foreach (CustomCharInfo info in CustomCharInfo.Values)
                info.ClearUser();
            preValueDic.Clear();
            CountryCommercialityDatas.Clear();
            SuperElecLine = null;
            AMJ7_PDI = 0;
            priceIsUpdateBySS = -1;
        }
        #region 存档持久化

        /// <summary>
        /// 本插件在存档 ModsData 中的数据键名。
        /// </summary>
        const string ModsDataKey = "SpecialRatizens";

        /// <summary>
        /// 读档时从存档 ModsData 恢复的保底计数（鼠名 → pdr_C）。
        /// </summary>
        static readonly Dictionary<string, int> PersistedPity = new Dictionary<string, int>();

        /// <summary>
        /// 存档 ModsData 载荷。
        /// </summary>
        class ModsSavePayload
        {
            public int version = 1;

            public Dictionary<string, int> pity = new Dictionary<string, int>();
        }

        /// <summary>
        /// 读档完成（D_Data 反序列化之后、地图加载之前）：
        /// 恢复持久化的保底计数，并按存档市民数据把「已拥有 ∪ 留有遗体」的特殊鼠鼠提前标记为已消耗，
        /// 使 SysMgr 步骤的洞列表重建不再把已拥有的鼠鼠滚入候选列表（修复同一存档重复招募的时序竞态）。
        /// </summary>
        public static void PlayDataMgr_LoadData(D_Data data)
        {
            if (data == null)
                return;

            PersistedPity.Clear();

            Utility.Savable.SavableData mods = data.ModsData;

            if (mods != null && mods.HasKey(ModsDataKey))
            {
                string json = mods.GetValue<string>(ModsDataKey, null);

                try
                {
                    ModsSavePayload payload = string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<ModsSavePayload>(json);

                    if (payload != null && payload.pity != null)
                    {
                        foreach (KeyValuePair<string, int> pair in payload.pity)
                            PersistedPity[pair.Key] = pair.Value;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn($"特殊鼠鼠存档数据解析失败，保底计数按零处理：{ex.Message}");
                }
            }

            if (!ActiveCustomSpecialUnit)
                return;

            //恢复保底计数（招募时 AddSpecialCitizen 会清零，已拥有的鼠鼠存档值恒为 0，互不冲突）
            foreach (KeyValuePair<string, int> pair in PersistedPity)
            {
                if (TryGetSpecialUnit(pair.Key, out CustomSpecialUnit unit))
                    unit.pdr_C = pair.Value;
            }

            //按存档数据推导已消耗集合：存活市民 ∪ 死亡名单（遗体仍在时不重新招募，遗体消失后即可再次招募）
            HashSet<string> consumed = new HashSet<string>();

            AppendConsumedNames(data.List_Citizen, consumed);

            AppendConsumedNames(data.List_DeathCitizen, consumed);

            if (consumed.Count == 0)
                return;

            foreach (CustomSpecialUnit unit in CustomSpecialUnitDatas.Values)
            {
                if (consumed.Contains(SpecialNamePolicy.Normalize(unit.name)))
                    unit.isUsed = true;
            }

            ModLog.Info($"特殊鼠鼠读档恢复完成：已消耗标记 {consumed.Count} 项，保底恢复 {PersistedPity.Count} 项");
        }

        /// <summary>
        /// 收集存档市民数据中的特殊鼠鼠名（存活名单与死亡名单共用）。
        /// </summary>
        static void AppendConsumedNames(List<Citizen_Data> list, HashSet<string> consumed)
        {
            if (list == null)
                return;

            foreach (Citizen_Data citizen in list)
            {
                if (citizen == null || string.IsNullOrWhiteSpace(citizen.m_UnitName))
                    continue;

                consumed.Add(SpecialNamePolicy.Normalize(citizen.m_UnitName));
            }
        }

        /// <summary>
        /// 存档数据收集（PlayDataMgr.Save 末尾的 SetMods）：
        /// 把当前保底计数写入存档 ModsData，随存档持久化（修复读档清零保底）。
        /// </summary>
        public static void PlayDataMgr_SetMods()
        {
            if (!ActiveCustomSpecialUnit)
                return;

            D_Data data = PlayDataMgr.Instance != null ? PlayDataMgr.Instance.m_GameData : null;

            if (data == null)
                return;

            if (data.ModsData == null)
                data.ModsData = Utility.Savable.SavableData.Create();

            Dictionary<string, int> pity = new Dictionary<string, int>();

            foreach (CustomSpecialUnit unit in CustomSpecialUnitDatas.Values)
            {
                if (unit.pdr_C != 0)
                    pity[unit.name] = unit.pdr_C;
            }

            data.ModsData.AddData(ModsDataKey, JsonConvert.SerializeObject(new ModsSavePayload { pity = pity }));
        }

        #endregion
        #region 会话加载

        /// <summary>
        /// 加载市民数据
        /// </summary>
        static void LoadCitizenDatas()
        {
            SpecialCitizens.Clear();
            usedNames.Clear();

            SpecialCitizenSkins.Clear();

            specialUnit = null;

            foreach (CustomCharInfo info in CustomCharInfo.Values)
                info.ClearUser();


            for (int i = 0; i < Citizens.Count; i++)
            {
                T_Citizen citizen = Citizens[i];


                //添加市民名称
                if (!string.IsNullOrWhiteSpace(citizen.m_UnitName) && !SpecialNamePolicy.IsTaken(citizen.m_UnitName, usedNames))
                    usedNames.Add(SpecialNamePolicy.Normalize(citizen.m_UnitName));

                //特殊市民加载
                if (!TryGetSpecialUnit(citizen, out CustomSpecialUnit unit) || !AddSpecialCitizen(unit, citizen))
                {
    
                    continue;
                }

                ModLog.Info($"读档识别特殊鼠鼠 {unit.Name}");

                if (citizen.m_Power < unit.pow)
                    citizen.m_Power = unit.pow;

                if (citizen.m_Dex < unit.dex)
                    citizen.m_Dex = unit.dex;

                if (citizen.m_Int < unit.wit)
                    citizen.m_Int = unit.wit;

                citizen.NameUpdate();

                foreach (CharacterInfo info in citizen.List_CharInfoValue)
                {
                    UpdateCustomCharInfoUser(info.Name, citizen);
                }
            }

            //存档中留有遗体的特殊鼠鼠同样视为已消耗并保留其名字，
            //避免普通市民占用名字导致遗体消失后该鼠鼠永远无法再次招募。
            List<Citizen_Data> deathList = GameData != null ? GameData.List_DeathCitizen : null;

            if (deathList != null)
            {
                foreach (Citizen_Data dead in deathList)
                {
                    if (dead == null || !TryGetSpecialUnit(dead.m_UnitName, out CustomSpecialUnit deadUnit) || deadUnit.isUsed)
                        continue;

                    deadUnit.isUsed = true;

                    string deadNormalizedName = SpecialNamePolicy.Normalize(deadUnit.name);

                    if (deadNormalizedName.Length > 0 && !SpecialNamePolicy.IsTaken(deadNormalizedName, usedNames))
                        usedNames.Add(deadNormalizedName);

                    ModLog.Info($"特殊鼠鼠 {deadUnit.name} 留有遗体，暂不重新出现");
                }
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 自定义角色特性
        /// </summary>
        static Dictionary<string, CharacterInfo> CustomCharacterInfoDatas = new Dictionary<string, CharacterInfo>();
        /// <summary>
        /// 自定义特殊单位
        /// </summary>
        static Dictionary<string, CustomSpecialUnit> CustomSpecialUnitDatas = new Dictionary<string, CustomSpecialUnit>();
        /// <summary>
        /// 自定义特殊单位随机组
        /// </summary>
        static Dictionary<int, List<CustomSpecialUnit>> CustomSpecialUnitRandomGroup = new Dictionary<int, List<CustomSpecialUnit>>();
        /// <summary>
        /// 特殊市民
        /// </summary>
        static Dictionary<string, T_Citizen> SpecialCitizens = new Dictionary<string, T_Citizen>();

        /// <summary>
        /// 自定义特性
        /// </summary>
        static Dictionary<string, CustomCharInfo> CustomCharInfo = new Dictionary<string, CustomCharInfo>
        {
            { "NaiNai_Wisdom", new CustomCharInfo(C_Buff.Exp_Up) },
            { "NaiNai_Benevolence", new CustomCharInfo(C_Buff.HappyUp) },
            { "LB_Sad", new CustomCharInfo(C_Buff.HappyDown) },
            { "LB_Hope", new CustomCharInfo(C_Buff.HappyUp) },
            { "SY_KCL", new CustomCharInfo(C_Buff.HappyUp) },
            { "SY_QL", new CustomCharInfo(C_Buff.None) },
            { "YF_YJQ", new CustomCharInfo(C_Buff.None) },
            { "YF_YJJ", new CustomCharInfo(C_Buff.PowerUp) },
            { "HT_SYZS", new CustomCharInfo(C_Buff.None) },
            { "HT_WQX", new CustomCharInfo(C_Buff.HappyUp) },
            { "WH_NC", new CustomCharInfo(C_Buff.None) },
            { "WH_SZ", new CustomCharInfo(C_Buff.None) },
            { "BG_NYQY", new CustomCharInfo(C_Buff.None) },
            { "BG_SS", new CustomCharInfo(C_Buff.None) },
            { "LLJ_KYSS", new CustomCharInfo(C_Buff.None) },
            { "LLJ_LY", new CustomCharInfo(C_Buff.HappyUp) },
            { "DZ_MGJZ", new CustomCharInfo(C_Buff.None) },
            { "DZ_MGZL", new CustomCharInfo(C_Buff.HappyUp) },
            { "ZY_QTSPQ", new CustomCharInfo(C_Buff.None) },
            { "ZY_LD", new CustomCharInfo(C_Buff.DexUp) },
            { "PKQ_SWFT", new CustomCharInfo(C_Buff.None) },
            { "PKQ_DQCD", new CustomCharInfo(C_Buff.None) },
            { "AMJ7_LZDW", new CustomCharInfo(C_Buff.None) },
            { "AMJ7_LZJX", new CustomCharInfo(C_Buff.None) },
        };

        ///// <summary>
        ///// 初始特性1列表
        ///// </summary>
        //static List<CharacterInfo> DefaultChar1List = new List<CharacterInfo>();
        ///// <summary>
        ///// 初始特性2列表
        ///// </summary>
        //static List<CharacterInfo> DefaultChar2List = new List<CharacterInfo>();
        /// <summary>
        /// 所有特性
        /// </summary>
        static List<CharacterInfo> AllCharInfos = new List<CharacterInfo>();
        /// <summary>
        /// 初始特性1数量
        /// </summary>
        static int DefaultChar1Count = 0;
        /// <summary>
        /// 初始特性2数量
        /// </summary>
        static int DefaultChar2Count = 0;

        #region 更多名称

        static string[][] CustomNames_Female;
        static string[][] PerNames_Female
        {
            get
            {
                if (CustomNames_Female == null)
                {
                    CustomNames_Female = new string[2][];

                    CustomNames_Female[0] = NameTables.FemaleOneChar;

                    CustomNames_Female[1] = NameTables.FemaleTwoChar;
                }

                return CustomNames_Female;
            }
        }

        static string[][] CustomNames_Male;
        static string[][] PerNames_Male
        {
            get
            {
                if (CustomNames_Male == null)
                {
                    CustomNames_Male = new string[2][];

                    CustomNames_Male[0] = NameTables.MaleOneChar;

                    CustomNames_Male[1] = NameTables.MaleTwoChar;
                }

                return CustomNames_Male;
            }
        }

        /// <summary>
        /// 单次移民中所有用到的名字
        /// </summary>
        static List<string> tempUsedNames = new List<string>();

        /// <summary>
        /// 获得随机姓名
        /// </summary>
        /// <param name="_gender"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool CitizenCaveUI_GetRandomName(Gender _gender, ref string __result)
        {
            if (!ActiveCustomNames)
                return true;

            string name;

            do
            {
                string surName = CustomSurNames[RandomInt(0, CustomSurNames.Length)];

                int index = RandomInt(0, 2);

                string[] perNames = _gender == Gender.Female ? PerNames_Female[index] : PerNames_Male[index];

                name = $"{surName}{perNames[RandomInt(0, perNames.Length)]}";
            }
            while (tempUsedNames.IndexOf(name) != -1 || usedNames.IndexOf(name) != -1);

            tempUsedNames.Add(name);

            __result = name;

            return false;
        }

        /// <summary>
        /// 生成移民列表
        /// </summary>
        public static void CitizenCaveUI_MakeCitizenList_CustomName()
        {
            if (!ActiveCustomNames)
                return;

            tempUsedNames.Clear();
        }

        /// <summary>
        /// 生成市民
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_info"></param>
        public static void T_Citizen_MakeCtizen_ByCC_CustomName(T_Citizen __instance, CCMake_Info _info)
        {
            if (!ActiveCustomNames || TryGetSpecialUnit(__instance, out _))
                return;

            usedNames.Add(_info.Name);

            ModLog.Debug($"添加了自定义市民 {_info.Name}");
        }

        #endregion

        /// <summary>
        /// 已被占用的单位名（含普通市民与特殊鼠鼠）
        /// </summary>
        static List<string> usedNames = new List<string>();


        /// <summary>
        /// 繁荣数据
        /// </summary>
        static List<ProspertiyInfo> ProsperityDB = new List<ProspertiyInfo>();
        /// <summary>
        /// 当前繁荣基线所属的数据管理器。
        /// </summary>
        static DB_Mgr ProsperityDBOwner = null;
        /// <summary>
        /// 防止繁荣原始表暂不可用时重复刷屏。
        /// </summary>
        static bool ProsperityBaselineFailureLogged = false;

        /// <summary>
        /// 加载繁荣数据
        /// </summary>
        static bool LoadProsperityDB(DB_Mgr manager)
        {
            ProsperityDB.Clear();
            ProsperityDBOwner = null;

            if (manager == null || manager.m_Prosperity_DB1 == null ||
                manager.m_Prosperity_DB1.sheets == null || manager.m_Prosperity_DB1.sheets.Count == 0 ||
                manager.m_Prosperity_DB1.sheets[0] == null || manager.m_Prosperity_DB1.sheets[0].list == null)
            {
                LogProsperityBaselineFailure("原始繁荣数据尚未加载");
                return false;
            }

            foreach (Prosperity_DB1.Param db in manager.m_Prosperity_DB1.sheets[0].list)
            {
                if (db.Level == 0)
                    continue;

                ProsperityDB.Add(new ProspertiyInfo(db));
            }

            ProsperityDB.Sort((ProspertiyInfo x1, ProspertiyInfo x2) => x1.Level.CompareTo(x2.Level));

            if (ProsperityDB.Count == 0)
            {
                LogProsperityBaselineFailure("原始繁荣数据没有有效等级");
                return false;
            }

            ProsperityDBOwner = manager;
            ProsperityBaselineFailureLogged = false;
            return true;
        }

        /// <summary>
        /// 确保秦律始终使用当前数据库实例的原始繁荣基线。
        /// </summary>
        static bool EnsureProsperityBaseline()
        {
            DB_Mgr manager;
            try
            {
                manager = DBMgr;
            }
            catch (Exception error)
            {
                LogProsperityBaselineFailure($"数据管理器不可用：{error.GetType().Name}");
                return false;
            }

            if (manager == null || manager.List_ProsperityDB == null)
            {
                LogProsperityBaselineFailure("运行时繁荣数据不可用");
                return false;
            }

            List<int> liveLevels = manager.List_ProsperityDB.Select(info => info.Level).ToList();
            List<int> baselineLevels = ProsperityDB.Select(info => info.Level).ToList();
            if (ReferenceEquals(ProsperityDBOwner, manager) &&
                ProsperityBaselinePolicy.Matches(liveLevels, baselineLevels))
            {
                return true;
            }

            return LoadProsperityDB(manager) &&
                ProsperityBaselinePolicy.Matches(
                    manager.List_ProsperityDB.Select(info => info.Level).ToList(),
                    ProsperityDB.Select(info => info.Level).ToList());
        }

        static void LogProsperityBaselineFailure(string reason)
        {
            if (ProsperityBaselineFailureLogged)
                return;

            ProsperityBaselineFailureLogged = true;
            ModLog.Error($"繁荣等级基线初始化失败：{reason}；本次跳过秦律更新");
        }
        /// <summary>
        /// 加载特性设置
        /// </summary>
        /// <param name="__instance"></param>
        public static void DB_Mgr_Character_DB_Setting(DB_Mgr __instance)
        {

            AllCharInfos.Clear();

            AllCharInfos.AddRange(__instance.m_CharacterDB.List_Char1_DB);

            AllCharInfos.AddRange(__instance.m_CharacterDB.List_Char2_DB);

            LoadDefaultCharList();

            LoadCustomDatas();

            preValueDic.Clear();

            LoadProsperityDB(__instance);
        }

        /// <summary>
        /// 加载初始特性列表
        /// </summary>
        static void LoadDefaultCharList()
        {
            if (DefaultChar1Count > 0)
                return;

            DefaultChar1Count = DBMgr.m_CharacterDB.List_Char1_DB.Count;

            DefaultChar2Count = DBMgr.m_CharacterDB.List_Char2_DB.Count;

            ModLog.Info($"已加载 {DefaultChar1Count} 特性1 {DefaultChar2Count} 特性2");
        }

        /// <summary>
        /// 加载自定义数据
        /// </summary>
        static void LoadCustomDatas()
        {
            if (CustomRatizenCatalog == null)
                throw new InvalidDataException("特殊鼠鼠数据尚未初始化。未修改游戏特性数据库。");

            //加载自定义特性（每次会话重建实例：游戏读档会原地清空 ScriptableObject 中已注册特性的
            //显示字段（T_Name/Description/Icon），而启动时缓存的 RuntimeTraits 对象与 DB 条目是同一批引用，
            //复用它们会让 UpdateCharDB「已存在分支」的回填变成自赋值，导致详情面板特性名/描述空白。
            //SpecialTraitDefinition 持有解析时固化的不可变字符串，用它重建可恢复旧版「每会话全新对象」的语义。）
            List<CharacterInfo> customInfos = new List<CharacterInfo>();

            foreach (SpecialTraitDefinition trait in CustomRatizenCatalog.Traits)
            {
                customInfos.Add(new CharacterInfo
                {
                    Category = trait.Category,
                    Name = trait.Name,
                    T_Name = trait.DisplayName,
                    EffectValue_A = trait.EffectValueA,
                    EffectValue_B = trait.EffectValueB,
                    Description = trait.Description
                });
            }

            //加载特殊单位
            List<CustomSpecialUnit> customUnits = new List<CustomSpecialUnit>(CustomRatizenCatalog.RuntimeRatizens);

            CustomCharacterInfoDatas.Clear();

            CustomSpecialUnitDatas.Clear();

            CustomSpecialUnitRandomGroup.Clear();

            ModLog.Info($"共加载 {customInfos.Count} 自定义特性，{customUnits.Count} 自定义单位");

            //添加自定义特性
            foreach (CharacterInfo customInfo in customInfos)
            {
                CustomCharacterInfoDatas.Add(customInfo.Name, customInfo);
            }

            foreach (CustomSpecialUnit customUnit in customUnits)
            {
                if (customUnit.lockStatus == CustomSpecialUnit.Lock_Status.Lock)
                    continue;

                if (!CustomCharacterInfoDatas.TryGetValue(customUnit.char1, out CharacterInfo char1))
                {
                    ModLog.Warn($"自定义单位 {customUnit.name} 特性 {customUnit.char1} 配置错误！");

                    continue;
                }

                if (!CustomCharacterInfoDatas.TryGetValue(customUnit.char2, out CharacterInfo char2))
                {
                    ModLog.Warn($"自定义单位 {customUnit.name} 特性 {customUnit.char2} 配置错误！");

                    continue;
                }

                customUnit.char_1 = char1;

                customUnit.char_2 = char2;

                customUnit.pdr_C = 0;

                customUnit.isUsed = false;

                CustomSpecialUnitDatas.Add(customUnit.name, customUnit);

                if (!CustomSpecialUnitRandomGroup.TryGetValue(customUnit.grade, out List<CustomSpecialUnit> list))
                {
                    list = new List<CustomSpecialUnit>()
                    {
                        customUnit
                    };

                    CustomSpecialUnitRandomGroup.Add(customUnit.grade, list);
                }
                else
                    list.Add(customUnit);

                RegisterCustomCharInfo(char1, customUnit.icon1);

                RegisterCustomCharInfo(char2, customUnit.icon2);

                ModLog.Info($"自定义单位 {customUnit.name} 注册完毕，特性1 {char1.T_Name} 特性2 {char2.T_Name}");
            }
        }

        /// <summary>
        /// 注册自定义特性
        /// </summary>
        /// <param name="info"></param>
        /// <param name="iconAddress"></param>
        static void RegisterCustomCharInfo(CharacterInfo info, string iconAddress = "")
        {
            //正常情况下已预制
            if (!TryGetCustomCharInfo(info.Name, out CustomCharInfo charInfo))
            {
                charInfo = new CustomCharInfo(C_Buff.None);

                CustomCharInfo.Add(info.Name, charInfo);
            }

            charInfo.name = info.Name;

            charInfo.iconAddress = iconAddress;

            charInfo.t_name = info.T_Name;

            charInfo.value1 = info.EffectValue_A;

            charInfo.value2 = info.EffectValue_B;

            charInfo.description = info.Description;

            UpdateCharDB(info);

            RegisterCustomInfoIcon(info);
        }

        /// <summary>
        /// 更新特性数据库
        /// List_Char1_DB 数据目前使用了ScriptableObject，每次读档后会丢失汉字数据？
        /// 原特性通过 DefaultChar1List、DefaultChar2List 在第一加载时保存
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        static void UpdateCharDB(CharacterInfo info)
        {
            List<CharacterInfo> charList = info.Category == 0 ? DBMgr.m_CharacterDB.List_Char1_DB : DBMgr.m_CharacterDB.List_Char2_DB;

            int lastIndex = charList[charList.Count - 1].Index;

            string infoName = info.Name;

            //获得列表中相同的特性
            List<CharacterInfo> list = charList.Where(t => t.Name.Equals(infoName)).ToList();

            //添加特性进数据表
            if (list.Count == 0)
            {
                //绑定下标为末尾+1
                info.Index = lastIndex + 1;

                lastIndex++;

                charList.Add(info);
            }
            else
            {
                info.Index = list[0].Index;

                list[0].T_Name = info.T_Name;

                list[0].Description = info.Description;
            }
        }

        /// <summary>
        /// 注册自定义特性图标
        /// </summary>
        /// <param name="list"></param>
        /// <param name="lastIndex"></param>
        /// <param name="info"></param>
        static void RegisterCustomInfoIcon(CharacterInfo info)
        {
            if (info == null || !TryGetCustomCharInfo(info.Name, out CustomCharInfo customInfo))
                throw new InvalidDataException("注册特殊能力图标时找不到特性数据。");

            string iconKey = CustomIconKeys.ForTrait(info.Name);
            string indexKey = CustomIconKeys.ForCharacterIndex(info.Index);

            customInfo.iconKey = iconKey;

            string spriteName = customInfo.iconAddress;

            if (string.IsNullOrWhiteSpace(spriteName))
                throw new InvalidDataException($"特殊能力 {info.Name} 的图标地址为空。");

            string iconPath = Path.Combine(CustomDataPath, "Icon", $"{spriteName}.png");

            Sprite sprite = BaseCommand.LoadSpriteFromTexture2D(BaseCommand.LoadTextureFromFile(iconPath));

            if (sprite == null)
                throw new InvalidDataException($"特殊能力 {info.Name} 图标加载失败：{iconPath}");

            CharacterInfo indexedInfo = DBMgr.GetCharacterInfo(info.Index);

            if (indexedInfo == null || !string.Equals(indexedInfo.Name, info.Name, StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"特殊能力 {info.Name} 的图标索引 {info.Index} 被其他特性占用：{indexedInfo?.Name ?? "<null>"}");

            Dictionary<string, Sprite> sprites = DicSprits;

            if (sprites == null)
                throw new InvalidDataException($"特殊能力 {info.Name} 无法访问游戏图标资源表。");

            sprites[iconKey] = sprite;
            sprites[indexKey] = sprite;
        }

        /// <summary>
        /// 添加特殊市民
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="citizen"></param>
        static bool AddSpecialCitizen(CustomSpecialUnit unit, T_Citizen citizen)
        {
            if (unit == null)
            {
                ModLog.Error($"添加特殊市民 {citizen.m_UnitName} 错误");

                return false;
            }

            citizen.m_UnitName = unit.Name;

            if (SpecialCitizens.ContainsKey(unit.Name))
            {
                ModLog.Warn($"特殊市民 {citizen.m_UnitName} 已存在！");

                return false;
            }

            SpecialCitizens.Add(citizen.m_UnitName, citizen);

            //标记单位已出现，并让后续普通姓名生成也避开该名称。
            unit.isUsed = true;
            string normalizedName = SpecialNamePolicy.Normalize(unit.name);
            if (normalizedName.Length > 0 && !SpecialNamePolicy.IsTaken(normalizedName, usedNames))
                usedNames.Add(normalizedName);

            unit.pdr_C = 0;

            citizen.m_SkinInfo.m_Gender = citizen.m_Gender;

            RegisterCustomSkin(citizen.m_SkinInfo, unit, true);

            ModLog.Info($"获得特殊市民 {citizen.m_UnitName}，三维 {GetPDIValue(citizen, 2f)}，特性1 {unit.char_1.T_Name} : {unit.char_1.Description}，特性2 {unit.char_2.T_Name} : {unit.char_2.Description}");
            return true;
        }

        /// <summary>
        /// 市民是特殊单位
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool CitizenIsSpecialUnit(string name, int _id, out T_Citizen citizen)
        {
            if (!SpecialCitizens.TryGetValue(name, out citizen))
                return false;

            return citizen.m_ID == _id;
        }

        /// <summary>
        /// 市民是特殊单位
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool CitizenIsSpecialUnit(T_Citizen citizen, string name = "")
        {
            return SpecialCitizens.TryGetValue(citizen.m_UnitName, out _) && (name.Equals("") || citizen.m_UnitName.Contains(name));
        }

        /// <summary>
        /// 市民有特性
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="infoName"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        static bool CitizenHaveCharacterInfo(T_Citizen citizen, string infoName, out CharacterInfo info)
        {
            List<CharacterInfo> list = citizen.List_CharInfoValue.Where(t => t.Name.Equals(infoName)).ToList();

            info = list.Count > 0 ? list[0] : null;

            return info != null;
        }

        /// <summary>
        /// 更新自定义特性使用者
        /// </summary>
        /// <param name="name"></param>
        /// <param name="user"></param>
        static CustomCharInfo UpdateCustomCharInfoUser(string name, T_Citizen user)
        {
            if (!TryGetCustomCharInfo(name, out CustomCharInfo customInfo))
            {
                ModLog.Warn($"{user.m_UnitName} 自定义特性 {name} 获取失败");

                return null;
            }

            customInfo.User = user;

            return customInfo;
        }

        /// <summary>
        /// 待使用的特殊单位
        /// </summary>
        static CustomSpecialUnit specialUnit;
        /// <summary>
        /// 生成移民列表
        /// </summary>
        public static void CitizenCaveUI_MakeCitizenList()
        {
            if (!ActiveCustomSpecialUnit)
                return;

            List<CustomSpecialUnit> units = CustomSpecialUnitDatas.Values.ToList();
            List<SpecialCandidateState> states = units.Select(unit =>
                new SpecialCandidateState(
                    unit.name,
                    unit.grade,
                    unit.probability,
                    unit.isUsed || SpecialNamePolicy.IsTaken(unit.name, usedNames))
                {
                    ProbabilityBonus = unit.pdr_C
                }).ToList();

            SpecialCandidateState selected = SpecialSelectionEngine.Select(states, ProsperityLevel, RandomInt);

            for (int i = 0; i < units.Count; i++)
                units[i].pdr_C = states[i].ProbabilityBonus;

            specialUnit = selected == null
                ? null
                : units.First(unit => unit.name.Equals(selected.Name, StringComparison.Ordinal));

            if (specialUnit != null)
                ModLog.Debug($"出现特殊单位 {specialUnit.name}，当前概率 {specialUnit.RealProbability}/10000");
        }

        /// <summary>
        /// 生成角色信息
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_grade_max"></param>
        /// <returns></returns>
        public static bool CCMake_Info(CCMake_Info __instance, int _grade_max, bool _religion_check = false)
        {
            if (ActiveCustomSpecialUnit && specialUnit != null)
            {
                // The immigration preview can be rebuilt before the previous candidate is recruited.
                // Do not create another citizen whose visible name already exists in this save.
                if (SpecialNamePolicy.IsTaken(specialUnit.name, Citizens.Select(citizen => citizen.m_UnitName)))
                {
                    ModLog.Debug($"跳过重复特殊鼠鼠名称 {specialUnit.name}");
                    specialUnit = null;
                    return true;
                }
                __instance.List_CharInfo = new List<int>();

                __instance.m_Gender = specialUnit.gender;
                __instance.Name = specialUnit.Name;
                __instance.Power = specialUnit.pow;
                __instance.Dex = specialUnit.dex;
                __instance.Int = specialUnit.wit;
                __instance.CitizenGold = specialUnit.gold;
                __instance.m_Religion = Religion.None;
                __instance.MakeSkinInfo();
                __instance.List_CharInfo.Add(specialUnit.char_1.Index);
                __instance.List_CharInfo.Add(specialUnit.char_2.Index);

                RegisterCustomSkin(__instance.SkinInfo, specialUnit, false);

                ModLog.Info($"创建了自定义角色 {__instance.Name}，特性1 [{specialUnit.char_1.Index}]{specialUnit.char_1.T_Name} 特性2 [{specialUnit.char_2.Index}]{specialUnit.char_2.T_Name}");

                AudioController.PlayUIOneShot("SFX_UI_Popup_Casting", 1f, false, null);

                specialUnit = null;

                return false;
            }


            return true;
        }

        /// <summary>
        /// 更新所有已启用的特性效果
        /// </summary>
        static void UpdateAllUsedSpecialEffects()
        {
            UpdateAllSelfSpecialEffects();

            for (int i = 0; i < Citizens.Count; i++)
            {
                UpdateCitizenUsedSpecialEffects(Citizens[i]);
            }
        }

        /// <summary>
        /// 更新所有单体特性效果
        /// </summary>
        static void UpdateAllSelfSpecialEffects()
        {
            SY_QL_Effect();

            AMJ7_LZDW_Effect();

            AMJ7_LZJX_Effect();
        }

        /// <summary>
        /// 更新单个市民已启用的特性效果
        /// </summary>
        static void UpdateCitizenUsedSpecialEffects(T_Citizen citizen)
        {
            #region 自身效果

            //联邦的哀伤
            UpdateSelfSpecialState(citizen, "LB_Sad");
            //联邦的希望
            UpdateSelfSpecialState(citizen, "LB_Hope");

            //奈奈的智慧
            UpdateSelfSpecialState(citizen, "NaiNai_Wisdom");

            //龙胆
            ZY_LD_Effect(citizen);

            #endregion

            #region 群体效果

            //奈奈的关爱
            UpdateSpecialState("NaiNai_Benevolence", NNBen_Value, citizen);

            //垦草令
            UpdateSpecialState("SY_KCL", KCL_Value, citizen);

            //岳家军
            YF_YJJ_Effect(citizen);

            //五禽戏
            HT_WQX_Effect(citizen);

            //梨园
            LLJ_LY_Effect(citizen);

            #endregion
        }

        public static void GBot_MakeCitizen(GBot __instance, int _index)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            AMJ7_LZJX_Effect(__instance);
        }
        /// <summary>
        /// 通过移民生成市民
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_info"></param>
        public static void T_Citizen_MakeCtizen_ByCC(T_Citizen __instance, CCMake_Info _info)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            UpdateCitizenUsedSpecialEffects(__instance);

            //跳过非特殊移民
            if (!TryGetSpecialUnit(__instance, out CustomSpecialUnit unit))
                return;

            AddSpecialCitizen(unit, __instance);

            //重置同等级下的其他单位的概率增值
            if (CustomSpecialUnitRandomGroup.TryGetValue(unit.grade, out List<CustomSpecialUnit> groupList))
                groupList.ForEach(t => t.pdr_C = 0);

            //这里只针对群体效果
            foreach (int index in _info.List_CharInfo)
            {
                CharacterInfo info = DBMgr.GetCharacterInfo(index);

                if (info == null)
                {
                    ModLog.Warn($"{_info.Name} 特性 {index} 加载失败！");

                    continue;
                }

                CustomCharInfo customInfo = UpdateCustomCharInfoUser(info.Name, __instance);

                switch (info.Name)
                {
                    //奈奈的仁爱
                    case "NaiNai_Benevolence":

                        UpdateSpecialStateToAllCitizen(customInfo.c_Buff, customInfo.name, NNBen_Value);

                        break;

                    //联邦的哀伤
                    case "LB_Sad":
                    //联邦的希望
                    case "LB_Hope":

                        __instance.m_Buff.BuffRefSet(customInfo.c_Buff, customInfo.name, C_Buff_Category.None, info.EffectValue_A, -999, true);

                        break;

                    //秦律
                    case "SY_QL":

                        SY_QL_Effect();

                        break;

                    //岳家军
                    case "YF_YJJ":

                        for (int i = 0; i < Citizens.Count; i++)
                        {
                            YF_YJJ_Effect(Citizens[i], false);
                        }

                        break;

                    //五禽戏
                    case "HT_WQX":

                        for (int i = 0; i < Citizens.Count; i++)
                        {
                            HT_WQX_Effect(Citizens[i], false);
                        }

                        break;

                    //梨园
                    case "LLJ_LY":

                        LLJ_LY_Effect();

                        break;

                    //量子电网
                    case "AMJ7_LZDW":

                        AMJ7_LZDW_Effect();

                        break;

                    //量子机械
                    case "AMJ7_LZJX":

                        AMJ7_LZJX_Effect();

                        break;
                }
            }

            // 特性使用者已经全部登记，此时再应用一次自身与群体状态，
            // 避免新招募角色（例如奈奈酱的“奈奈的智慧”）必须读档后才生效。
            UpdateCitizenUsedSpecialEffects(__instance);
        }

        #region 特性效果部分
        /// <summary>
        /// 量子机械值
        /// </summary>
        static int LZJX_Value
        {
            get
            {
                return ActiveCustomSpecialUnit && TryGetCustomValue("奥米伽-7", "AMJ7_LZJX", out _, out T_Citizen citizen, out _, out _) ? GetPDIValue(citizen) : 0;
            }
        }
        /// <summary>
        /// 奥米伽三维
        /// </summary>
        static int AMJ7_PDI = 0;
        /// <summary>
        /// 量子机械
        /// </summary>
        static void AMJ7_LZJX_Effect(GBot bot = null)
        {
            string key = "AMJ7_LZJX";

            bool canUse = TryGetCustomValue("奥米伽-7", key, out _, out T_Citizen citizen, out float value1, out float value2);

            List<GBot> list = bot != null ? new List<GBot> { bot } : UnitMgr.List_GBot;

            AMJ7_PDI = canUse ? GetPDIValue(citizen) : 0;

            float value = canUse ? (value1 + AMJ7_PDI / value2) / 100f : 0f;

            float[] addValues = canUse ? new float[] { citizen.m_Power * value, citizen.m_Dex * value, citizen.m_Int * value } : null;

            //多状态放在Effect进行迭代
            for (int i = 0; i < list.Count; i++)
            {
                list[i].m_Buff.RefKill(key);

                if (!canUse)
                    continue;

                UpdateSpecialStateToUnit(list[i], C_Buff.PowerUp, key, Mathf.FloorToInt(addValues[0]));
                UpdateSpecialStateToUnit(list[i], C_Buff.DexUp, key, Mathf.FloorToInt(addValues[1]), false, false);
                UpdateSpecialStateToUnit(list[i], C_Buff.IntUp, key, Mathf.FloorToInt(addValues[2]), false, false);

                FillUpGbotPower(list[i]);
            }

            if (bot != null)
                ModLog.Debug($"机械 {bot.m_UnitName} 与奥米伽-7连接！");
            else
                ModLog.Debug($"共 {UnitMgr.List_GBot.Count} 机械与奥米伽-7连接！");
        }
        /// <summary>
        /// 补满机械鼠电力
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        static bool FillUpGbotPower(GBot bot)
        {
            if (bot == null || SuperElecLine == null || SuperElecLine.m_Watt <= 0f)
                return false;

            float value = SystemMgr.GetGBotMaxFatigue() - bot.m_Fatigue;

            if (value == 0f)
                return true;

            //补满电力
            if (value > 0 && SuperElecLine.UseWatt(-1, -value))
            {
                bot.FatigueUpate(value);

                if (bot.m_CharState == CharState.Injury)
                {
                    bot.ImFatigueSet(0);

                    bot.SetCharState(CharState.None);

                    bot.SetAniState(AniState.Idle, "Idle_GBot", true, true);

                    ModLog.Debug($"机械 {bot.m_UnitName} 已倒地！");
                }

                ModLog.Debug($"机械 {bot.m_UnitName} 通过连接奥米伽-7补充了 {value} 体力！");

                return true;
            }
            //补充少量电力
            else if (bot.m_Fatigue < 10)
            {
                value = SuperElecLine.m_Watt > 10 ? 10 : SuperElecLine.m_Watt;

                if (SuperElecLine.UseWatt(-1, -value))
                {
                    bot.FatigueUpate(10);

                    ModLog.Debug($"机械 {bot.m_UnitName} 通过连接奥米伽-7补充了 {value} 体力！");

                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 机械体力更新
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool GBot_FatigueUpate(GBot __instance, float value)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZJX") || SuperElecLine == null || __instance.m_ImFatigue != 0 || value > 0f)
                return true;

            float num = 1f + (__instance.m_Buff.GetBuffValue(C_Buff.SLP_Up) + __instance.m_Buff.GetBuffValue(C_Buff.SLP_Down));

            if (num != 0f)
            {
                if (num < 0f)
                {
                    num = 0.01f;
                }
                value *= num;
            }

            //维持消耗
            if (!SuperElecLine.UseWatt(-1, value))
                return true;

            FillUpGbotPower(__instance);

            return false;
        }
        /// <summary>
        /// 电网添加耗电信息
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ElecLine_Info_AddConnectUseBuild(ElecLine_Info __instance, int _id, float _value)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZJX") || _id < 0)
                return true;

            __instance.m_HourUseWatt += _value;

            return false;
        }
        /// <summary>
        /// 电网获得电力
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_useid"></param>
        /// <param name="_value"></param>
        /// <returns></returns>
        public static void ElecLine_Info_AddWatt(ElecLine_Info __instance, float _value)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZJX") || _value <= 0 || SuperElecLine == null)
                return;

            //自动为机械鼠补充电力
            foreach (GBot bot in UnitMgr.List_GBot)
            {
                if (!FillUpGbotPower(bot))
                    break;
            }
        }

        /// <summary>
        /// 超级电网
        /// </summary>
        static ElecLine_Info SuperElecLine = null;
        /// <summary>
        /// 量子电网
        /// </summary>
        static void AMJ7_LZDW_Effect()
        {
            SuperElecLine = null;

            if (!CustomCharInfoIsActive("AMJ7_LZDW"))
                return;

            CombineAllElecLine();

            BuildingMgr.RefreshElecUseBuilding();

            ModLog.Info($"量子电网已启动！");
        }
        /// <summary>
        /// 合并所有电网
        /// </summary>
        static void CombineAllElecLine()
        {
            List<ElecLine_Info> elecLineList = BuildingMgr.List_ElecInfo;

            int count = elecLineList.Count;

            while (elecLineList.Count > 1)
            {
                BuildingMgr.MergeTwoElecLine(elecLineList[0], elecLineList[elecLineList.Count - 1]);
            }

            ModLog.Info($"超级电网合并完成，共合并 {count} 电网");

            if (count == 0)
                return;

            SuperElecLine = elecLineList[0];
        }
        /// <summary>
        /// 基类建筑电路检查
        /// </summary>
        /// <param name="__instance"></param>
        public static void Building_WireCheck(Building __instance, bool _use, ref bool __result)
        {
            SuperElecLineWireCheck(__instance, _use, ref __result);
        }
        /// <summary>
        /// 电力建筑电路检查
        /// </summary>
        /// <param name="__instance"></param>
        public static void Building_ElecMasonry_WireCheck(Building_ElecMasonry __instance, bool _use, ref bool __result)
        {
            SuperElecLineWireCheck(__instance, _use, ref __result);
        }
        /// <summary>
        /// 电力物流建筑电路检查
        /// </summary>
        /// <param name="__instance"></param>
        public static void Building_ElecCarrierStation_WireCheck(Building_ElecCarrierStation __instance, bool _use, ref bool __result)
        {
            SuperElecLineWireCheck(__instance, _use, ref __result);
        }
        /// <summary>
        /// 电力舞台建筑电路检查
        /// </summary>
        /// <param name="__instance"></param>
        public static void Building_ElecBandstand_WireCheck(Building_ElecBandstand __instance, bool _use, ref bool __result)
        {
            SuperElecLineWireCheck(__instance, _use, ref __result);
        }
        /// <summary>
        /// 超级电网电路检查
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_use"></param>
        /// <param name="__result"></param>
        static void SuperElecLineWireCheck(Building __instance, bool _use, ref bool __result)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZDW") || SuperElecLine == null || __result)
                return;

            if (!__instance.m_Activation || __instance.m_BuildState == BuildState.NeedRepair || __instance.m_BuildState == BuildState.NeedGround || __instance.m_BuildState == BuildState.IsFlood)
                return;

            List<int> idList = SuperElecLine.Dic_Storage.Keys.ToList();

            Building_Battery building_Battery = idList.Count > 0 ? BuildingMgr.List_Battery.Find(t => t.m_ID.Equals(idList[0])) : null;

            if (building_Battery != null)
            {
                __instance.m_ElecWire.WireSet(__instance.Tf.position, building_Battery.Tf.position, building_Battery);

                BuildingMgr.ConnectUseBuild(__instance.m_ID, building_Battery.m_ID, __instance.m_Info.ElecCost);

                if (_use && building_Battery.UseWatt(__instance.m_ID, __instance.m_Info.ElecCost))
                {
                    if (__instance.m_BuildAlarm.m_State == BuildState.NoElec || __instance.m_BuildAlarm.m_State == BuildState.NoBattery)
                    {
                        __instance.m_BuildState = BuildState.Basic;

                        __instance.AlarmSet(BuildState.Basic);
                    }
                    __instance.m_ElecNum = 1;

                    __result = true;

                    ModLog.Debug($"{__instance.m_CustomName} 处于电网范围外，已单独加入超级电网！");
                }
            }
        }
        /// <summary>
        /// 获取周围电网（针对添加时）
        /// </summary>
        /// <param name="__instance"></param>
        public static bool BuildingMgr_GetFourDir_ElecGroup(ElecPort _port, ref List<ElecLine_Info> __result)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZDW") || SuperElecLine == null)
                return true;

            __result = new List<ElecLine_Info> { SuperElecLine };
            return false;
        }
        /// <summary>
        /// 建筑电力删除连接检查
        /// </summary>
        /// <param name="__instance"></param>
        public static bool BuildingMgr_DeleteConnectCheck(BuildingMgr __instance, int _id, List<ElecPort> _list_port)
        {
            if (!CustomCharInfoIsActive("AMJ7_LZDW") || SuperElecLine == null)
                return true;

            for (int i = 0; i < _list_port.Count; i++)
            {
                Vector2Int vector2Int = new Vector2Int(_list_port[i].m_X, _list_port[i].m_Y);
                if (__instance.Dic_PortTileMap.ContainsKey(vector2Int))
                {
                    __instance.Dic_PortTileMap.Remove(vector2Int);
                }
                __instance.RefreshWire(vector2Int);
            }

            ElecLine_Info elecLine = __instance.SearchElecInfo(_id);

            if (elecLine != null)
                elecLine.RemoveKey(_id);

            List<int> list = (elecLine != null) ? elecLine.FindAnotherStorageList(_id) : new List<int>() { };

            ModLog.Debug($"({_id}) 所在电网 {(elecLine != null ? elecLine.m_CustomName : "无")} 与 {list.Count} 电池相连，触发删除检测，当前共 {BuildingMgr.List_ElecInfo.Count} 电网");

            //CombineAllElecLine();

            return false;
        }
        /// <summary>
        /// 电网使用电力
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_useid"></param>
        /// <param name="_value"></param>
        /// <returns></returns>
        public static void ElecLine_Info_UseWatt(ElecLine_Info __instance, ref float _value)
        {
            if (!TryGetCustomValue("奥米伽-7", "AMJ7_LZDW", out _, out _, out float value1, out _) || _value > 0f)
                return;

            float costRatio = 1 - AMJ7_PDI / value1 / 100f;

            costRatio = costRatio < 0f ? 0f : costRatio;

            _value *= costRatio;
        }

        /// <summary>
        /// 电气场地
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="overflowPower"></param>
        static void PKQ_DQCD_Effect(T_Citizen citizen, float overflowPower)
        {
            if (overflowPower <= 0f)
                return;

            string key = "PKQ_DQCD";

            if (!ActiveCustomSpecialUnit || !CitizenHaveCharacterInfo(citizen, key, out CharacterInfo info))
                return;

            citizen.m_Fatigue += overflowPower;

            float value = citizen.m_Dex * info.EffectValue_A / 100f;

            int time = (int)(overflowPower / info.EffectValue_B);

            for (int i = 0; i < Citizens.Count; i++)
            {
                UpdateSpecialStateToUnit(Citizens[i], C_Buff.SpdUp, key, value, Citizens[i] == citizen, true, time == 0 ? 1 : time);
            }

            ModLog.Debug($"{citizen.m_UnitName} 溢出电力 {overflowPower} 获得状态 {info.T_Name}");
        }
        /// <summary>
        /// 十万伏特
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="info"></param>
        static void PKQ_SWFT_Effect(T_Citizen citizen, Building_ThermalGenerator building)
        {
            if (building == null || !TryGetCustomCharInfo("PKQ_SWFT", out CustomCharInfo info))
                return;

            int value = IntProbability;
            float thrValue = (citizen.m_Power + citizen.m_Int) * info.value1;



            //十万伏特
            if (value > thrValue)
                return;

            int ratio = RandomInt(citizen.m_Int / (int)info.value1, citizen.m_Int * (int)info.value1 + 1);
            float power = citizen.m_Power * ratio;

            ElecLine_Info elecLine_Info = GameMgr.Instance._BuildingMgr.SearchElecInfo(building.m_ID);
            if (elecLine_Info == null)
            {
                ModLog.Warn($"皮卡丘发电失败：建筑 {building.m_CustomName} 未连接电网");
                return;
            }

            //溢出的电力
            float overflowPower = elecLine_Info.m_Watt + power - elecLine_Info.m_MaxWatt;
            elecLine_Info.AddWatt(power);
            IndividualStatisticsManager.Instance.Add(GameMgr.Instance._SysMgr.m_Day, building.m_ID, power, new string[]
            {
            "Electricity",
            "Product",
            "Value"
            });

            ModLog.Info($"皮卡丘触发了{ratio}倍十万伏特，产生了 {power} 电力");

            PKQ_DQCD_Effect(citizen, overflowPower);

            //损坏
            if (IntProbability <= info.value2 - citizen.m_Int)
            {
                building.m_CurHP = 0f;

                building.SetNeedRepair(true);

                ModLog.Warn($"皮卡丘的十万伏特损坏了建筑 {building.m_CustomName}");
            }
        }
        /// <summary>
        /// 建筑工作进度更新后
        /// 工作完成后
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="d_time"></param>
        public static void MasonryInfo_WorkUpdate_Postfix(MasonryInfo __instance, ref float d_time)
        {
            if (!ActiveCustomSpecialUnit || __instance.m_CurTime != 0 || d_time == 0)
                return;

            Building building = __instance.m_Building;

            if (building == null)
                return;

            T_Citizen worker = building.m_Master;

            if (worker == null)
                return;

            ModLog.Debug($"{building.m_Info.T_Name}({building.m_Info.Name}) 完成工作 {d_time}");

            //皮卡丘在鼠力发电站完成工作时
            if (building.m_Info.Name == BuildingName.ManpowerGenerator && CitizenIsSpecialUnit(worker, "皮卡丘") && CustomCharInfoIsActive("PKQ_SWFT", out _))
            {
                PKQ_SWFT_Effect(worker, building as Building_ThermalGenerator);

                ModLog.Debug($"{building.m_Info.T_Name}({building.m_Info.Name})({building.GetType()}/{building is Building_ThermalGenerator}) {worker.m_UnitName}完成了发电工作 {d_time}，当前力量经验 {worker.m_PowerExp}");
            }
        }

        /// <summary>
        /// 七探蛇盘枪
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="unit"></param>
        /// <param name="info"></param>
        static void ZY_QTSPQ_Effect(T_Citizen citizen, GameUnit unit, CharacterInfo info)
        {
            //击退
            unit.Knockback(unit.Tf.position, false, 0, info.EffectValue_A);

            //减少50%移速
            UpdateSpecialStateToUnit(unit, C_Buff.SpdDown, "QTSPQ", 0.5f);

            ModLog.Debug($"{citizen.name} 击退了 {unit.name}");
        }
        /// <summary>
        /// 龙胆
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="usedDetection"></param>
        static void ZY_LD_Effect(T_Citizen citizen, CharacterInfo info = null)
        {
            string key = "ZY_LD";

            citizen.m_Buff.RefKill(key);

            if (!ActiveCustomSpecialUnit || (info == null && !CitizenHaveCharacterInfo(citizen, key, out info)))
                return;

            UpdateSpecialStateToUnit(citizen, C_Buff.SpdUp, key, info.EffectValue_A);
            UpdateSpecialStateToUnit(citizen, C_Buff.Dodge, key, info.EffectValue_B, false, false);

            ModLog.Debug($"{citizen.m_UnitName} 获得状态 {info.T_Name}");
        }

        /// <summary>
        /// 蘑菇之力
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="time"></param>
        static void DZ_MGZL_Effect(T_Citizen citizen, int time)
        {
            if (time == 0)
                return;

            string key = "DZ_MGZL";

            int value = citizen.m_Buff.GetRestHour(key);

            time = time < value ? value : time;

            UpdateSpecialStateToUnit(citizen, C_Buff.SLP_Down, key, -0.3f, true, true, time);
            UpdateSpecialStateToUnit(citizen, C_Buff.ProductivityUp, key, 0.1f, false, false, time);
            UpdateSpecialStateToUnit(citizen, C_Buff.SpdUp, key, 0.1f, false, false, time);
        }
        /// <summary>
        /// 应用食物或生活用品效果
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="t_info"></param>
        public static void T_Citizen_ApplyFoodOrLife_ResAbility(T_Citizen __instance, TileInfo t_info)
        {
            if (!CustomCharInfoIsActive("DZ_MGZL"))
                return;
            int time = 0;

            if (t_info.m_TileType == TileType.Mushroom)
                time = 18;
            else if (t_info.m_TileType == TileType.GrilledMushroom)
                time = 24;
            else if (t_info.m_TileType == TileType.Steak)
                time = 36;

            DZ_MGZL_Effect(__instance, time);
        }

        /// <summary>
        /// 蘑菇教主值
        /// </summary>
        static float DZMGJZ_Value
        {
            get
            {
                float value = ActiveCustomSpecialUnit && TryGetCustomValue("大正", "DZ_MGJZ", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out float value2) ? 1f + (value1 + (citizen.m_Int - unit.wit) * value2) / 100f : 1f;

                value = value < 0f ? 0f : value;

                return value;
            }
        }
        /// <summary>
        /// 建筑工作进度更新前
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="d_time"></param>
        public static void MasonryInfo_WorkUpdate_Prefix(MasonryInfo __instance, ref float d_time)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            //更新蘑菇农场的工作效率
            if (__instance.m_Building.m_Info.Name == BuildingName.MushroomFarm && CustomCharInfoIsActive("DZ_MGJZ", out _))
                d_time *= DZMGJZ_Value;
        }

        /// <summary>
        /// 梨园值2
        /// </summary>
        static float LLJLY_Value2
        {
            get
            {
                return TryGetCustomValue("李隆基", "LLJ_LY", out CustomSpecialUnit _, out T_Citizen citizen, out _, out float value2) ? float.Parse((citizen.m_Int * value2 / 100f).ToString("F2")) : 0f;
            }
        }
        /// <summary>
        /// 梨园值1
        /// </summary>
        static float LLJLY_Value1
        {
            get
            {
                float value = 0f;

                if (TryGetCustomValue("李隆基", "LLJ_LY", out CustomSpecialUnit _, out T_Citizen citizen, out float value1, out _))
                {
                    float intValue = citizen.m_Int - value1 < 1f ? 1f : citizen.m_Int - value1;

                    value = 1f - intValue / (intValue + 1);
                }

                return float.Parse(value.ToString("F2"));
            }
        }
        /// <summary>
        /// 梨园
        /// </summary>
        /// <param name="customInfo"></param>
        static void LLJ_LY_Effect(T_Citizen citizen = null)
        {
            string key = "LLJ_LY";

            bool canUse = CustomCharInfoIsActive(key);

            List<T_Citizen> list = citizen != null ? new List<T_Citizen> { citizen } : Citizens;

            //多状态放在Effect进行迭代
            for (int i = 0; i < list.Count; i++)
            {
                list[i].m_Buff.RefKill(key);

                //允许清理
                if (!canUse)
                    continue;

                UpdateSpecialStateToUnit(list[i], C_Buff.ProductivityUp, key, LLJLY_Value2);
                UpdateSpecialStateToUnit(list[i], C_Buff.FUN_Down, key, LLJLY_Value1, false, false);
            }
        }

        /// <summary>
        /// 开元盛世
        /// </summary>
        static int LLJ_KYSS_Value
        {
            get
            {
                int value = TryGetCustomValue("李隆基", "LLJ_KYSS", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out _) ? citizen.m_Int - unit.wit + Mathf.FloorToInt(ProsperityLevel / value1) : 0;

                return value;
            }
        }
        /// <summary>
        /// 获得最大访客数
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool Helpers_Get_MaximumGuestNum(BuildingName _name, ref int __result)
        {
            if (!ActiveCustomSpecialUnit)
                return true;

            if (_name == BuildingName.Atelier || _name == BuildingName.HairShop || _name == BuildingName.Laundry || _name == BuildingName.Hospital || _name == BuildingName.MassageBed || _name == BuildingName.Toilet || _name == BuildingName.GuardPost || _name == BuildingName.FleaCleaner)
            {
                __result = 1 + LLJ_KYSS_Value;
            }
            else if (_name == BuildingName.BugRacingTrack)
            {
                __result = 4;
            }
            else
                __result = 2 + LLJ_KYSS_Value;

            return false;
        }

        /// <summary>
        /// 商圣
        /// </summary>
        static float BGSS_Value
        {
            get
            {
                return TryGetCustomValue("白圭", "BG_SS", out CustomSpecialUnit _, out T_Citizen citizen, out float value1, out _) ? citizen.m_Int * value1 / 100f : 0f;
            }
        }
        /// <summary>
        /// 能以取予值
        /// </summary>
        static float BGNYQY_Value
        {
            get
            {
                float value = 0f;

                if (TryGetCustomValue("白圭", "BG_NYQY", out CustomSpecialUnit _, out T_Citizen citizen, out float value1, out _))
                {
                    float intValue = citizen.m_Int - value1 < 1f ? 1f : citizen.m_Int - value1;

                    value = intValue / (intValue + 1) - 1f;
                }

                return value;
            }
        }
        //static string baseValueText = "";
        //static string nyqyUpdateValueText = "";
        //static string ssUpdateValueText = "";
        /// <summary>
        /// 获得进口价格
        /// </summary>
        /// <param name="price"></param>
        /// <param name="nowRelations"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool DiplomaticCountryResourceData_TradeCountryToMyKingdomPrice(float price, int nowRelations, TileInfo ____info, ref float __result)
        {
            if (!ActiveCustomSpecialUnit)
                return true;

            if (____info == null)
            {
                ModLog.Warn("贸易物品获取失败！");

                return true;
            }

            float baseValue = price * (1.2f - (nowRelations - 70) / 200f);

            //baseValueText = baseValue.ToString();

            //nyqyUpdateValueText = "";

            //ssUpdateValueText = $"{SymbolText}{ColorText_F}{baseValue * GetSSValue(____info)}{ColorText_B}";

            float value = 1 + GetSSValue(____info);

            __result = baseValue * value;

            ModLog.Debug($"{____info.T_Name} 进口原价为 {baseValue} 最终进口价格为 {__result}，影响系数为 {value}");

            return false;
        }
        /// <summary>
        /// 获得出口价格
        /// </summary>
        /// <param name="price"></param>
        /// <param name="nowRelations"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool DiplomaticCountryResourceData_TradeMyKingdomToCountryPrice(float price, int nowRelations, TileInfo ____info, ref float __result)
        {
            if (!ActiveCustomSpecialUnit)
                return true;

            if (____info == null)
            {
                ModLog.Warn("贸易物品获取失败！");
                return true;
            }

            float baseValue = price * (0.8f + (nowRelations - 70) / 200f);

            //baseValueText = baseValue.ToString();

            //nyqyUpdateValueText = $" - <color=#C83232>{baseValue * Mathf.Abs(BGNYQY_Value)}</color>";

            //ssUpdateValueText = $"{SymbolText}{ColorText_F}{baseValue * GetSSValue(____info)}{ColorText_B}";

            float value = 1 + BGNYQY_Value + GetSSValue(____info);

            __result = baseValue * value;

            ModLog.Debug($"{____info.T_Name} 出口原价为 {baseValue} 最终出口价格为 {__result}，影响系数为 {value}");

            return false;
        }
        /// <summary>
        /// 价格受商圣影响，-1：无，0：增，1：减
        /// </summary>
        static int priceIsUpdateBySS = -1;
        static string SymbolText { get { return priceIsUpdateBySS == -1 ? "" : priceIsUpdateBySS == 0 ? " (涨)" : " (跌)"; } }
        static string ColorText_F { get { return priceIsUpdateBySS == 0 ? "<color=#1E8A00>" : priceIsUpdateBySS == 1 ? "<color=#C83232>" : ""; } }
        static string ColorText_B { get { return priceIsUpdateBySS > -1 ? "</color>" : ""; } }
        /// <summary>
        /// 获得商圣影响值
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        static float GetSSValue(TileInfo info)
        {
            if (info == null)
                return 0f;

            float ssValue = BGSS_Value, value = 0f;

            //日用品春降夏涨
            if (info.Category == ResCateogry.Life)
                value = WeatherMgr.m_SeasonState == SeasonState.Spring ? -ssValue : WeatherMgr.m_SeasonState == SeasonState.Summer ? ssValue : 0f;
            //食物秋降冬涨
            else if (info.Category == ResCateogry.Food)
                value = WeatherMgr.m_SeasonState == SeasonState.Fall ? -ssValue : WeatherMgr.m_SeasonState == SeasonState.Winter ? ssValue : 0f;

            priceIsUpdateBySS = value == 0 ? -1 : value > 0 ? 0 : 1;

            ModLog.Debug($"当前季节 {WeatherMgr.m_SeasonState} 贸易物品 {info.T_Name} 价格浮动 {value}({ssValue})");

            return value;
        }
        /// <summary>
        /// 商业值成长阈值
        /// </summary>
        static readonly int comValueGrowthThreshold = 1000;
        /// <summary>
        /// 城市最大繁荣值
        /// </summary>
        static readonly float maxCountryProsperityValue = 10000;
        /// <summary>
        /// 商业值增长最小系数值
        /// </summary>
        static readonly float comGrowthMinValue = 0.1f;
        /// <summary>
        /// 城市商业值数据
        /// </summary>
        static Dictionary<string, float> CountryCommercialityDatas = new Dictionary<string, float>();
        /// <summary>
        /// 贸易完成事件
        /// </summary>
        /// <param name="result"></param>
        /// <param name="__result"></param>
        public static void DiplomaticMgr_OnTradeResultEvent_BGNYQY(TradeResult result, TradeReceive __result)
        {
            if (!CustomCharInfoIsActive("BG_NYQY") || __result.TradeReceiveState != TradeReceiveState.Success)
                return;

            DiplomaticCountryTradeSheetData sheet = result.Sheet;

            DiplomaticCountryData country = sheet.CountryData;

            TypeTrade typeTrade = sheet.TypeTrade;

            //获得贸易价值
            float value = typeTrade == TypeTrade.Country_To_Hometown ? sheet.TotalTradeCountryToHometownPrice() : sheet.TotalTradeHometownToCountryPrice();

            //最终贸易价值
            value *= country.TypeMoney == TypeMoney.Dar ? 10f : 1f;

            bool haveComValue = CountryCommercialityDatas.TryGetValue(country.Key, out float comValue);

            //商业增长系数
            double factorValue = GetComValueGrowthFactor(country.NowProsperityValue);

            //商业增长值
            float comAddValue = (float)(value * factorValue);

            //累计商业值
            comValue += comAddValue;

            //繁荣增长值
            int addValue = Mathf.FloorToInt(comValue / comValueGrowthThreshold);

            //剩余商业值
            float remainingValue = comValue - comValueGrowthThreshold * addValue;

            if (!haveComValue)
                CountryCommercialityDatas.Add(country.Key, remainingValue);
            else
                CountryCommercialityDatas[country.Key] = remainingValue;

            ModLog.Info($"贸易完成，贸易价值 {value} 增长系数 {factorValue.ToString("f4")}，目标城市 {country.Name} 获得 {comAddValue}(累计 {comValue} - 消耗 {comValueGrowthThreshold * addValue} = 剩余 {remainingValue}) 商业值，繁荣提升了 {addValue} 点");
        }
        /// <summary>
        /// 获得商业值增长系数
        /// </summary>
        /// <param name="prosperityValue"></param>
        /// <returns></returns>
        static float GetComValueGrowthFactor(float prosperityValue)
        {
            prosperityValue = prosperityValue > maxCountryProsperityValue ? maxCountryProsperityValue : prosperityValue;

            return comGrowthMinValue + (1 - comGrowthMinValue) * (maxCountryProsperityValue - prosperityValue) / maxCountryProsperityValue;
        }

        /// <summary>
        /// 牛车值
        /// </summary>
        static float WHNC_Value
        {
            get
            {
                float value = ActiveCustomSpecialUnit && TryGetCustomValue("王亥", "WH_NC", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out float value2) ? 1f - (value1 + (citizen.m_Dex - unit.dex) * value2) / 100f : 1f;

                value = value < 0f ? 0f : value > 1f ? 1f : value;

                return value;
            }
        }
        /// <summary>
        /// 设置城市贸易距离
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="tInstance"></param>
        public static void DiplomaticData_SetTerrainTotalDistance(DiplomaticWorldTerrainEntity tInstance)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            int dis = tInstance.TotalDistanceValue;

            int newDis = Mathf.FloorToInt(dis * WHNC_Value);

            newDis = newDis <= 0 ? 1 : newDis;

            tInstance.SetTotalDistance(newDis);

            //foreach (DiplomaticCountryData country in ____countryDic.Values)
            //{ 
            //    country
            //}

            ModLog.Debug($"DiplomaticData: 设置目标城市 {tInstance.ID} 距离 {newDis}/{dis}");
        }

        /// <summary>
        /// 商祖值
        /// </summary>
        static int WHSZ_Value
        {
            get
            {
                return ActiveCustomSpecialUnit && TryGetCustomValue("王亥", "WH_SZ", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out _) ? Mathf.FloorToInt(citizen.m_Int - unit.wit + ProsperityLevel / value1) : 0;
            }
        }
        /// <summary>
        /// 获得最大贸易协议数量
        /// </summary>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool DiplomaticCountryData_MaxTradeAgreementCount(ref int __result)
        {
            if (!ActiveCustomSpecialUnit)
                return true;

            __result = 3 + WHSZ_Value;

            return false;
        }

        /// <summary>
        /// 五禽戏
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="check"></param>
        static void HT_WQX_Effect(T_Citizen citizen, bool check = true)
        {
            string key = "HT_WQX";

            citizen.m_Buff.RefKill(key);

            if (check && !CustomCharInfoIsActive(key))
                return;

            UpdateSpecialStateToUnit(citizen, C_Buff.MaxHP_Up, key, 50);
            UpdateSpecialStateToUnit(citizen, C_Buff.ProductivityUp, key, 0.2f, false, false);
            UpdateSpecialStateToUnit(citizen, C_Buff.SpdUp, key, 0.1f, false, false);
        }

        /// <summary>
        /// 岳家军
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="check"></param>
        static void YF_YJJ_Effect(T_Citizen citizen, bool check = true)
        {
            string key = "YF_YJJ";

            citizen.m_Buff.RefKill(key);

            if ((check && !CustomCharInfoIsActive(key)) || !CitizenIsSolider(citizen))
                return;

            UpdateSpecialStateToUnit(citizen, C_Buff.ATK_Up, key, 5);
            UpdateSpecialStateToUnit(citizen, C_Buff.DEF_Up, key, 3, false, false);
            UpdateSpecialStateToUnit(citizen, C_Buff.SpdUp, key, 0.3f, false, false);
        }
        /// <summary>
        /// 岳家枪
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="dmg"></param>
        /// <param name="name"></param>
        static void YF_YJQ_Effect(T_Citizen citizen, float dmg, CharacterInfo info, string name)
        {
            float ratio = info != null ? info.EffectValue_A : 30f;

            float healValue = Mathf.Abs(dmg * citizen.m_Dex / ratio);

            citizen.Heal(healValue, true);

            ModLog.Debug($"{citizen.name} 攻击了 {name}，造成 {-dmg} 伤害，恢复 {healValue} 生命");
        }

        /// <summary>
        /// 市民更新职业
        /// 岳家军
        /// </summary>
        public static void T_Citizen_JobSet(T_Citizen __instance)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            YF_YJJ_Effect(__instance);
        }

        /// <summary>
        /// 市民是士兵
        /// </summary>
        /// <param name="citizen"></param>
        /// <returns></returns>
        static bool CitizenIsSolider(T_Citizen citizen)
        {
            if (citizen == null || citizen.m_Job == null)
                return false;

            BuildingName buildingName = citizen.m_Job.m_Info.Name;

            //训练营地、兵营、战斗训练所、战争神殿、守护者大厅
            return buildingName == BuildingName.TrainingCamp || buildingName == BuildingName.Barrack || buildingName == BuildingName.CombatAcademy || buildingName == BuildingName.DruidCamp || buildingName == BuildingName.GuardianTemple;
        }

        /// <summary>
        /// 市民近战攻击
        /// 岳家枪 七探蛇盘枪
        /// </summary>
        public static void T_Citizen_SwdAtk_Call(T_Citizen __instance)
        {
            if (!ActiveCustomSpecialUnit)
                return;

            bool yjq = CitizenHaveCharacterInfo(__instance, "YF_YJQ", out CharacterInfo yqjInfo), qtspq = CitizenHaveCharacterInfo(__instance, "ZY_QTSPQ", out CharacterInfo qtspqInfo);

            if (!yjq && !qtspq)
                return;

            GameUnit targetUnit = __instance.m_TargetUnit;

            float dmg = __instance.GetDmg();

            if (targetUnit != null && __instance.m_AtkBox.IsCollide(targetUnit, false) && targetUnit.m_CharState != CharState.Death && targetUnit.m_CharState != CharState.Injury)
            {
                //装备长枪时
                if (__instance.m_WeaponName == WeaponName.Spear || __instance.m_WeaponName == WeaponName.AdvancedSpear || __instance.m_WeaponName == WeaponName.FlagSpear || __instance.m_WeaponName == WeaponName.SkirmisherSpear)
                {
                    for (int i = 0; i < UnitMgr.List_AllEnemy.Count; i++)
                    {
                        GameUnit unit = UnitMgr.List_AllEnemy[i];

                        bool hitTarget = __instance.m_AtkBox.IsCollide(unit, false);

                        //命中溅射单位
                        if (unit != targetUnit && hitTarget)
                        {
                            float rangeDmg = dmg * 0.5f;

                            if (yjq)
                                YF_YJQ_Effect(__instance, rangeDmg, yqjInfo, unit.name);
                            else if (qtspq)
                                ZY_QTSPQ_Effect(__instance, unit, qtspqInfo);

                            unit.BeAttacked(-rangeDmg, Unit_Attacekd_Tag.OurTeam, __instance.m_ID);
                        }
                    }
                }

                if (yjq)
                    YF_YJQ_Effect(__instance, dmg, yqjInfo, targetUnit.name);
                else if (qtspq)
                    ZY_QTSPQ_Effect(__instance, targetUnit, qtspqInfo);

                ModLog.Debug($"{__instance.name} 攻击了 {targetUnit.name}({targetUnit.GetType()})");
            }
        }

        /// <summary>
        /// 秦律值
        /// </summary>
        static int SYQL_Value
        {
            get
            {
                return ActiveCustomSpecialUnit && TryGetCustomValue("商鞅", "SY_QL", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out _) ? Mathf.FloorToInt(citizen.m_Int - unit.wit + ProsperityLevel / value1) : 0;
            }
        }
        /// <summary>
        /// 秦律：更新法典数量
        /// </summary>
        static void SY_QL_Effect()
        {
            if (!EnsureProsperityBaseline())
                return;

            int bonus = CustomCharInfoIsActive("SY_QL") ? SYQL_Value : 0;
            int[] values = ProsperityBaselinePolicy.ApplyBonus(
                ProsperityDB.Select(info => info.PolicyNum).ToArray(),
                bonus);

            for (int i = 0; i < DBMgr.List_ProsperityDB.Count; i++)
            {
                DBMgr.List_ProsperityDB[i].PolicyNum = values[i];
            }
        }
        /// <summary>
        /// 垦草令的值
        /// </summary>
        static int KCL_Value
        {
            get
            {
                int count = Citizens.Count;

                if (!ActiveCustomSpecialUnit || count == 0)
                    return 0;

                if (!TryGetCustomValue("商鞅", "SY_KCL", out _, out _, out float value1, out float value2))
                {
                    value1 = 10;

                    value2 = 30;
                }

                int ratio = Mathf.FloorToInt(SttMgr.m_FoodUI.m_FoodNum / count);

                int value = Mathf.FloorToInt(ratio / value1);

                return (int)(value > value2 ? value2 : value);
            }
        }
        /// <summary>
        /// 更新食物
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static void FoodUI_AllFood_Update()
        {
            int value = KCL_Value;

            //垦草令
            if (NeedUpdatePDIWithCustomCharInfo("SY_KCL", value, out CustomCharInfo info))
                UpdateSpecialStateToAllCitizen(info.c_Buff, info.name, value, false);
        }

        /// <summary>
        /// 上一次的值
        /// </summary>
        static Dictionary<string, float> preValueDic = new Dictionary<string, float>();
        /// <summary>
        /// 奈奈的关爱的值
        /// </summary>
        static int NNBen_Value
        {
            get
            {
                return ActiveCustomSpecialUnit && TryGetCustomValue("奈奈酱", "NaiNai_Benevolence", out CustomSpecialUnit unit, out T_Citizen citizen, out float value1, out _) ? GetPDIValue(citizen, value1) : 0;
            }
        }
        /// <summary>
        /// 更新三维
        /// </summary>
        /// <param name="__instance"></param>
        public static void GameUnit_UpdatePDI_Post()
        {
            //奈奈的希望
            float value = NNBen_Value;
            if (NeedUpdatePDIWithCustomCharInfo("NaiNai_Benevolence", value, out CustomCharInfo info))
                UpdateSpecialStateToAllCitizen(info.c_Buff, info.name, value, false);

            //秦律
            value = SYQL_Value;
            if (NeedUpdatePDIWithCustomCharInfo("SY_QL", value, out _))
                SY_QL_Effect();

            //梨园
            value = LLJLY_Value1 + LLJLY_Value2;
            if (NeedUpdatePDIWithCustomCharInfo("LLJ_LY", value, out _))
                LLJ_LY_Effect();

            //量子机械
            value = LZJX_Value;
            if (NeedUpdatePDIWithCustomCharInfo("AMJ7_LZJX", value, out _))
                AMJ7_LZJX_Effect();
        }

        #endregion

        #region 公共部分

        /// <summary>
        /// 需要更新自定义特性的三维值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nowValue"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        static bool NeedUpdatePDIWithCustomCharInfo(string key, float nowValue, out CustomCharInfo info)
        {
            return CustomCharInfoIsActive(key, out info) && !IsSameValue(key, nowValue);
        }

        /// <summary>
        /// 是相同的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nowValue"></param>
        /// <returns></returns>
        static bool IsSameValue(string key, float nowValue)
        {
            if (!preValueDic.TryGetValue(key, out float value))
            {
                preValueDic.Add(key, nowValue);

                return false;
            }
            else if (value != nowValue)
            {
                preValueDic[key] = nowValue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 市民恢复饱食度
        /// 联邦的希望
        /// </summary>
        /// <param name="__instance"></param>
        public static void T_Citizen_HungerUpdate(T_Citizen __instance, float value)
        {
            if (!ActiveCustomSpecialUnit || value < 0f || !CitizenHaveCharacterInfo(__instance, "LB_Hope", out CharacterInfo info))
                return;

            float happy = __instance.GetHappyValue();

            int minNum = GetPDIValue(__instance, info.EffectValue_B), maxNum = GetPDIValue(__instance, info.EffectValue_B / 1.5f), result = 0;

            for (int i = 0; i < maxNum; i++)
            {
                if (RandomFloat(0f, 100f) <= happy)
                    result += 1;
            }

            result = result < minNum ? minNum : result;



            CreateTileObj(TileType.Gold, __instance.GetPos(), result);
        }

        /// <summary>
        /// 更新自身的特殊状态
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="name"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        static void UpdateSelfSpecialState(T_Citizen citizen, string name, float? num = null)
        {
            if (CitizenHaveCharacterInfo(citizen, name, out _))
                UpdateSpecialState(name, num, citizen);
        }

        /// <summary>
        /// 更新特殊状态
        /// </summary>
        static bool UpdateSpecialState(string name, float? num = null, T_Citizen citizen = null)
        {
            float value = 0f;

            C_Buff? cBuff = null;

            if (CustomCharInfoIsActive(name, out CustomCharInfo info))
            {
                value = num ?? info.value1;

                cBuff = info.c_Buff;
            }

            //单独更新
            if (citizen != null)
                UpdateSpecialStateToUnit(citizen, cBuff, name, value);
            //全体更新
            else
                UpdateSpecialStateToAllCitizen(cBuff, info.name, value);

            return true;
        }

        /// <summary>
        /// 为所有市民更新特殊状态
        /// </summary>
        /// <param name="c_Buff"></param>
        /// <param name="refName"></param>
        /// <param name="value"></param>
        /// <param name="time_hour"></param>
        /// <param name="category"></param>
        /// <param name="show"></param>
        static void UpdateSpecialStateToAllCitizen(C_Buff? c_Buff, string refName, float value, bool show = true, bool kill = true, int time_hour = -999, C_Buff_Category category = C_Buff_Category.None)
        {
            for (int i = 0; i < Citizens.Count; i++)
            {
                UpdateSpecialStateToUnit(Citizens[i], c_Buff, refName, value, show, kill, time_hour, category);
            }
        }

        /// <summary>
        /// 为单位更新状态
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="c_Buff"></param>
        /// <param name="refName"></param>
        /// <param name="value"></param>
        /// <param name="show"></param>
        /// <param name="time_hour"></param>
        /// <param name="category"></param>
        static void UpdateSpecialStateToUnit(GameUnit unit, C_Buff? c_Buff, string refName, float value, bool show = true, bool kill = true, int time_hour = -999, C_Buff_Category category = C_Buff_Category.None)
        {
            if (kill)
                unit.m_Buff.RefKill(refName);

            if (ActiveCustomSpecialUnit && c_Buff != null)
                unit.m_Buff.BuffRefSet((C_Buff)c_Buff, refName, category, value, time_hour, show);
        }

        /// <summary>
        /// 设置状态图标
        /// </summary>
        /// <param name="__instance"></param>
        public static void BuffIcon_IconSet(BuffIcon __instance, BuffInfo _info)
        {
            if (!ActiveCustomSpecialUnit ||
                !TryGetCustomCharInfo(_info.ReferenceName, out CustomCharInfo customInfo))
                return;

            _info.T_Name = _info.ReferenceName;

            __instance.m_Spr.sprite = Func.Instance.LoadSprite(customInfo.iconKey);
        }

        /// <summary>
        /// 获得三维值
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        static int GetPDIValue(T_Citizen citizen, float ratio = 1f)
        {
            return citizen != null ? Mathf.FloorToInt((citizen.GetPDI(PDI.Power) + citizen.GetPDI(PDI.Dex) + citizen.GetPDI(PDI.Int)) / ratio) : 0;
        }

        /// <summary>
        /// 是自定义特性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool IsCustomCharInfo(string name)
        {
            return TryGetCustomCharInfo(name, out _);
        }

        /// <summary>
        /// 自定义特性是否启用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool CustomCharInfoIsActive(string name)
        {
            return ActiveCustomSpecialUnit && CustomCharInfoIsActive(name, out _);
        }

        /// <summary>
        /// 自定义特性是否启用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static bool CustomCharInfoIsActive(string name, out CustomCharInfo info)
        {
            return TryGetCustomCharInfo(name, out info) && info.IsActive;
        }

        /// <summary>
        /// 尝试获得自定义特性的值
        /// </summary>
        /// <param name="unitName"></param>
        /// <param name="charName"></param>
        /// <param name="unit"></param>
        /// <param name="user"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        static bool TryGetCustomValue(string unitName, string charName, out CustomSpecialUnit unit, out T_Citizen user, out float value1, out float value2)
        {
            user = null;

            value1 = 0f;

            value2 = 0f;

            if (TryGetSpecialUnit(unitName, out unit) && TryGetCustomCharInfo(charName, out CustomCharInfo info))
            {
                user = info.User;

                value1 = info.value1;

                value2 = info.value2;

                return user != null;
            }

            return false;
        }

        /// <summary>
        /// 尝试获得特殊鼠鼠
        /// </summary>
        /// <param name="unitName"></param>
        /// <param name="citizen"></param>
        /// <returns></returns>
        static bool TryGetSpecialCitizen(string unitName, out T_Citizen citizen)
        {
            citizen = null;

            return TryGetSpecialUnit(unitName, out CustomSpecialUnit unit) && SpecialCitizens.TryGetValue(unit.Name, out citizen);
        }

        /// <summary>
        /// 尝试获得特殊单位
        /// </summary>
        /// <param name="citizen"></param>
        /// <param name="customUnit"></param>
        /// <returns></returns>
        static bool TryGetSpecialUnit(T_Citizen citizen, out CustomSpecialUnit customUnit)
        {
            foreach (KeyValuePair<string, CustomSpecialUnit> keyValue in CustomSpecialUnitDatas)
            {
                //迭代市民的所有特性
                foreach (CharacterInfo info in citizen.List_CharInfoValue)
                {
                    //与自定义单位的特性匹配
                    if (info.Name.Equals(keyValue.Value.char1) || info.Name.Equals(keyValue.Value.char2))
                    {
                        customUnit = keyValue.Value;

                        ModLog.Warn($"市民 {customUnit.name} 是特殊单位 ，特性1 {customUnit.char_1.Name}/{keyValue.Value.char1}，特性2 {customUnit.char_2.Name}/{keyValue.Value.char2}");

                        return true;
                    }
                }
            }

            customUnit = null;

            return false;
        }

        /// <summary>
        /// 尝试获得特殊单位
        /// </summary>
        /// <param name="name"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        static bool TryGetSpecialUnit(string name, out CustomSpecialUnit unit)
        {
            return CustomSpecialUnitDatas.TryGetValue(name, out unit);
        }

        /// <summary>
        /// 尝试获得自定义特性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="charInfo"></param>
        /// <returns></returns>
        static bool TryGetCustomCharInfo(string name, out CustomCharInfo charInfo)
        {
            return CustomCharInfo.TryGetValue(name, out charInfo);
        }

        /// <summary>
        /// 尝试根据名称获得自定义特性
        /// </summary>
        /// <param name="t_name"></param>
        /// <param name="charInfo"></param>
        /// <returns></returns>
        static bool TryGetCustomCharInfoByTName(string t_name, out CustomCharInfo charInfo)
        {
            List<CustomCharInfo> list = CustomCharInfo.Select(t => t.Value).Where(t => t.t_name.Equals(t_name)).ToList();

            charInfo = list.Count > 0 ? list[0] : null;

            return list.Count > 0;
        }

        /// <summary>
        /// 获得图标地址
        /// </summary>
        /// <param name="_RefName"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool RefInfo_GetIconAddress(string _RefName, ref string __result)
        {
            if (!ActiveCustomSpecialUnit || !TryGetCustomCharInfo(_RefName, out CustomCharInfo customInfo))
                return true;

            __result = customInfo.iconKey;

            return false;
        }

        /// <summary>
        /// 获得状态名称
        /// </summary>
        /// <param name="_RefName"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        public static bool RefInfo_Get_T_Name(string _RefName, ref string __result)
        {
            if (!ActiveCustomSpecialUnit || !TryGetCustomCharInfo(_RefName, out CustomCharInfo customInfo))
                return true;

            __result = customInfo.t_name;

            return false;
        }

        /// <summary>
        /// 获取市民状态描述
        /// </summary>
        /// <param name="info"></param>
        public static bool CitizenBuff_RefInfo_GetDescript(CitizenBuff.RefInfo __instance, ref string __result)
        {
            if (!ActiveCustomSpecialUnit || !TryGetCustomCharInfo(__instance.RefName, out CustomCharInfo charInfo))
                return true;

            __result = charInfo.description;

            return false;
        }

        #endregion

        #endregion
        #region 特性生成边界

        /// <summary>
        /// 生成角色特性
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        public static bool CCMake_Info_MakeCharacterList(CCMake_Info __instance)
        {
            return MakeCharacterList(__instance);
        }

        /// <summary>
        /// 生成特性列表
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        static bool MakeCharacterList(CCMake_Info info)
        {
            if (info.List_CharInfo == null)
                info.List_CharInfo = new List<int>();


            info.List_CharInfo.Add(DBMgr.m_CharacterDB.List_Char1_DB[RandomInt(0, DefaultChar1Count)].Index);

            info.List_CharInfo.Add(DBMgr.m_CharacterDB.List_Char2_DB[RandomInt(0, DefaultChar2Count)].Index);

            return false;
        }

        #endregion
        #region 战斗修正（神医在世、量子机械）

        /// <summary>
        /// 市民受到伤害
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="_tag"></param>
        /// <returns></returns>
        public static bool T_Citizen_BeAttacked(T_Citizen __instance, ref float dmg, Unit_Attacekd_Tag _tag)
        {

            float value = dmg;

            //启用了神医在世，且不在受伤状态下
            if (CustomCharInfoIsActive("HT_SYZS") && __instance.m_CharState != CharState.Injury)
            {
                float injuryThreshold = __instance.m_MaxHP * 0.2f;

                //最大伤害为可致受伤
                float dmgLimit = __instance.m_CurHP > injuryThreshold ? __instance.m_CurHP - injuryThreshold : 0;

                dmg = dmg < -dmgLimit ? -dmgLimit : dmg;
            }

            //奥米伽不会受到致死伤害
            if (CitizenHaveCharacterInfo(__instance, "AMJ7_LZJX", out _))
                dmg = __instance.m_CurHP + dmg <= 0 ? 0 : dmg;
            return true;
        }

        #endregion

        #region 自定义皮肤

        /// <summary>
        /// 市民可自定义的皮肤部位名称列表
        /// </summary>
        /// <summary>
        /// 自定义部位默认值
        /// </summary>
        static readonly Dictionary<string, string> CustomCategoryDefaultValues = new Dictionary<string, string>()
        {
            { "Face", "Face_1" }, { "Skin", "White" }, { "Hair_Male", "Hair_1" }, { "Hair_Female", "Hair_2" }, { "Dress_Male", "Dress_29" }, { "Dress_Female", "Dress_28" }
        };
        /// <summary>
        /// 特殊鼠鼠的皮肤
        /// </summary>
        static Dictionary<string, Dictionary<string, string>> SpecialCitizenSkins = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// 市民更新默认服装前
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static bool T_Citizen_DefaultClothesUpdate(T_Citizen __instance)
        {
            if (!UpdateClothes(__instance))
                return true;

            ModLog.Debug($"单位 {__instance.m_UnitName} 更新了默认服装");

            return false;
        }

        /// <summary>
        /// 单位更新服装前
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static bool GameUnit_ClothesUpdate(GameUnit __instance, int num)
        {
            //非工作时
            if (num != 0 || !UpdateClothes(__instance))
                return true;
            return false;
        }

        /// <summary>
        /// 更新服装
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        static bool UpdateClothes(GameUnit unit)
        {
            if (!CitizenIsSpecialUnit(unit.m_UnitName, unit.m_ID, out T_Citizen citizen) ||
                !TryGetSpecialUnit(citizen, out CustomSpecialUnit specialUnit) ||
                !SpecialCitizenSkins.TryGetValue(citizen.m_UnitName, out Dictionary<string, string> customSkin))
                return false;

            citizen.m_SkinInfo.m_Gender = citizen.m_Gender;

            bool updated = UpdateUnitSpineDress(
                citizen.m_SkinInfo,
                citizen.m_UnitName,
                citizen.m_Gender.ToString(),
                citizen.m_Job,
                true,
                customSkin);

            ModLog.Debug($"特殊单位 {specialUnit.Name} {(updated ? "更新" : "恢复")}了服装");
            return updated;
        }
        /// <summary>
        /// 注册自定义皮肤
        /// </summary>
        /// <param name="unit"></param>
        static void RegisterCustomSkin(Sp_SkinInfo skinInfo, CustomSpecialUnit unit, bool applyToSkeleton)
        {
            string key = unit.Name;
            string gender = skinInfo.m_Gender.ToString();

            SpecialCitizenSkins[key] = new Dictionary<string, string>() { { "Skin", unit.skin.Trim() }, { "Face", unit.face.Trim() }, { "Bread", unit.bread.Trim() }, { "Dress", unit.dress.Trim() }, { "Glasses", unit.glasses.Trim() }, { "Hair", unit.hair.Trim() }, { "Hat", unit.hat.Trim() }, { "Makeup", unit.makeup.Trim() } };

            ModLog.Debug($"特殊皮肤 {key} 注册：实际模板性别 {gender}");

            UpdateUnitSpineDress(skinInfo, key, gender, null, true, SpecialCitizenSkins[key], applyToSkeleton);
        }

        /// <summary>
        /// 更新单位皮肤
        /// </summary>
        /// <param name="skinInfo"></param>
        /// <param name="key"></param>
        /// <param name="gender"></param>
        /// <param name="job"></param>
        /// <param name="isCitizen"></param>
        /// <param name="customSkin"></param>
        static bool UpdateUnitSpineDress(Sp_SkinInfo skinInfo, string key, string gender, Building job, bool isCitizen, Dictionary<string, string> customSkin, bool applyToSkeleton = true)
        {
            SpineDresserBundle bundle = SpineDresserMgr.Instance.Bundle;

            bool hasKey = bundle.HasKey(key);

            bool havePermanent = customSkin.TryGetValue("Basic", out string value) && !value.Equals("");

            TryGetJobPairs(job, gender, out SpineDresserPair[] jobPairs);

            //皮肤元素：模版->部位组合
            SpineDresserElement element = SpineDresserElement.Create(key);

            //模版：男/女
            SpineDresserTemplete templete = SpineDresserTemplete.Create(gender);

            templete.Pairs = new SpineDresserPair[customSkin.Count];

            int index = 0;

            foreach (KeyValuePair<string, string> customPair in customSkin)
            {
                //当设置了预设皮肤时，其他皮肤显示为未设置状态
                string pair = SkinPairCorrection(gender, customPair.Key, customPair.Value, havePermanent, jobPairs);

                templete.Pairs[index] = SpineDresserPair.Create(customPair.Key, pair);
                index++;
            }

            SetStructPrivateValue(ref element, "_templetes", new SpineDresserTemplete[] { templete });

            bool isAdd = false;

            //更新皮肤
            if (hasKey)
            {
                //迭代所有皮肤
                for (int i = 0; i < bundle.Elements.Length; i++)
                {
                    if (bundle.Elements[i].Key.Equals(element.Key))
                    {
                        bundle.Elements[i] = element;

                        break;
                    }
                }

                ModLog.Debug($"{key} 更新了皮肤");

                //SetPrivateValue(bundle, "_elements", bundle.Elements);
            }
            //添加皮肤
            else
            {
                isAdd = true;

                SetPrivateValue(bundle, "_elements", bundle.Elements.AddToArray(element));
            }
            
            ModLog.Debug($"{key} {(isAdd ? "添加" : "更新")}了皮肤，当前共有 {bundle.Elements.Length} 个皮肤");

            // && !CitizenCaveUI.Obj_Main.activeSelf

            return UpdateUnitCustomSkin(skinInfo, key, isCitizen, applyToSkeleton);
        }

        /// <summary>
        /// 部位皮肤修正
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="havePermanent"></param>
        /// <returns></returns>
        static string SkinPairCorrection(string gender, string key, string value, bool havePermanent, SpineDresserPair[] jobPairs)
        {
            //预制皮肤使用预制配置
            if (havePermanent)
                return key.Equals("Basic") ? value : "";

            if (!value.Equals(""))
                return value;

            //使用职业皮肤配置
            if (jobPairs != null)
            {
                foreach (SpineDresserPair pair in jobPairs)
                {
                    //存在该部位且有配置时
                    if (pair.Category.Equals(key) && !pair.Skin.Equals(""))
                    {
                        ModLog.Debug($"部位 {pair.Category} 使用了职业皮肤 {pair.Skin}");

                        return pair.Skin;
                    }
                }
            }

            if (key.Equals("Hair"))
                key = $"Hair_{gender}";

            if (key.Equals("Dress"))
                key = $"Dress_{gender}";

            return CustomCategoryDefaultValues.TryGetValue(key, out string corValue) ? corValue : "";
        }

        /// <summary>
        /// 获取职业皮肤配置
        /// </summary>
        /// <param name="job"></param>
        /// <param name="gender"></param>
        /// <param name="jobPairs"></param>
        /// <returns></returns>
        static bool TryGetJobPairs(Building job, string gender, out SpineDresserPair[] jobPairs)
        {
            jobPairs = null;

            if (job == null)
                return false;

            string jobKey = job.m_Info.Name.ToString();

            if (job.m_Info.Ability == BuildAbility.Barrack && job.m_BuildInfoUI.IsProductEnable())
            {
                MilitaryInfo militaryInfo = GameMgr.Instance._DB_Mgr.m_MilitaryDB._list.Find((MilitaryInfo x) => x.Index == job.m_BuildInfoUI.GetProductIndex());

                if (militaryInfo != null)
                    jobKey = militaryInfo.SkinName;
            }

            return TryGetSpineDresserElement(jobKey, out SpineDresserElement jobSkin) && jobSkin.TryGetPairs(gender, out jobPairs);
        }

        /// <summary>
        /// 尝试获取皮肤
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        static bool TryGetSpineDresserElement(string key, out SpineDresserElement result)
        {
            List<SpineDresserElement> list = SpineDresserMgr.Instance.Bundle.Elements.Where(t => t.Key.Equals(key)).ToList();

            result = list.FirstOrDefault();

            return list.Count > 0;
        }

        /// <summary>
        /// 更新单位自定义皮肤
        /// </summary>
        /// <param name="skinInfo"></param>
        /// <param name="key"></param>
        /// <param name="isCitizen"></param>
        static bool UpdateUnitCustomSkin(Sp_SkinInfo skinInfo, string key, bool isCitizen = true, bool applyToSkeleton = true)
        {
            Dictionary<string, string> skinSnapshot = new Dictionary<string, string>(skinInfo.SkinDic);
            Dictionary<string, string> overrideSnapshot = new Dictionary<string, string>(skinInfo.OverrideSkinDic);
            string gender = skinInfo.m_Gender.ToString();

            if (!TryGetSpineDresserElement(key, out SpineDresserElement element) ||
                !element.TryGetPairs(gender, out SpineDresserPair[] pairs) ||
                pairs == null)
            {
                if (isCitizen)
                    RecoverUnitSkin(skinInfo, skinSnapshot, overrideSnapshot, key, $"缺少 {gender} 模板", applyToSkeleton);
                else
                    ModLog.Error($"特殊皮肤 {key} 组合失败：缺少 {gender} 模板");

                return false;
            }

            if (isCitizen)
                skinInfo.ClearSkins();

            SpineDresserMgr.Instance.AssembleData(key, skinInfo, false);

            if (isCitizen && !SkinRepairPolicy.HasRequiredAppearance(skinInfo.SkinDic))
            {
                string missing = string.Join(",", SkinRepairPolicy.MissingRequiredCategories(skinInfo.SkinDic));
                RecoverUnitSkin(skinInfo, skinSnapshot, overrideSnapshot, key, $"缺少关键部件 {missing}", applyToSkeleton);
                return false;
            }

            RenderCombinedSkin(skinInfo, applyToSkeleton);
            return true;
        }

        /// <summary>
        /// 特殊皮肤更新失败时恢复可见外观。
        /// </summary>
        static void RecoverUnitSkin(Sp_SkinInfo skinInfo, Dictionary<string, string> skinSnapshot, Dictionary<string, string> overrideSnapshot, string key, string reason, bool applyToSkeleton)
        {
            SkinRecoveryKind recovery = SkinRepairPolicy.SelectRecovery(skinSnapshot);

            skinInfo.ClearSkins();
            skinInfo.ClearOverrideSkin();

            if (recovery == SkinRecoveryKind.Snapshot)
            {
                skinInfo.SetStyles(skinSnapshot, null);

                foreach (KeyValuePair<string, string> pair in overrideSnapshot)
                    skinInfo.SetStyleOverride(pair.Key, pair.Value);
            }
            else
            {
                SpineDresserMgr.Instance.AssembleDefaultSkin(skinInfo);
                SpineDresserMgr.Instance.AssembleData("Jobless_1_1", skinInfo, true);
            }

            RenderCombinedSkin(skinInfo, applyToSkeleton);

            string recoveryName = recovery == SkinRecoveryKind.Snapshot ? "原外观" : "原版默认外观";
            ModLog.Error($"特殊皮肤 {key} 组合失败：{reason}；已使用 {recoveryName} 恢复");

            if (!SkinRepairPolicy.HasRequiredAppearance(skinInfo.SkinDic))
            {
                string missing = string.Join(",", SkinRepairPolicy.MissingRequiredCategories(skinInfo.SkinDic));
                ModLog.Error($"特殊皮肤 {key} 恢复后仍缺少关键部件：{missing}");
            }
        }

        /// <summary>
        /// 将组合皮肤安装到当前 Spine 骨架。
        /// </summary>
        static void RenderCombinedSkin(Sp_SkinInfo skinInfo, bool applyToSkeleton)
        {
            skinInfo.SkinSet(skinInfo.m_Skin, skinInfo.m_SkeletonData);

            if (applyToSkeleton)
                skinInfo.UpdateCombinedSkin();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取私有变量值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static T GetPrivateValue<T>(object instance, string fieldName)
        {
            return Traverse.Create(instance).Field(fieldName).GetValue<T>();
        }

        /// <summary>
        /// 设置私有变量值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static void SetPrivateValue<T>(object instance, string fieldName, T value)
        {
            Traverse.Create(instance).Field(fieldName).SetValue(value);
        }

        /// <summary>
        /// 设置结构私有变量值
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public static void SetStructPrivateValue<T1, T2>(ref T1 instance, string fieldName, T2 value) where T1 : struct
        {
            object obj = instance;

            Traverse.Create(obj).Field(fieldName).SetValue(value);

            instance = (T1)obj;
        }
        /// <summary>
        /// 创建物品
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="num"></param>
        /// <param name="objState"></param>
        static void CreateTileObj(TileType type, Vector3 pos, int num = 1, TObjState objState = TObjState.Basic, bool fadeSkip = false)
        {
            if (num == 0)
                return;

            PoolMgr.Pool_TileObject.GetNextObj().GetComponent<TileObject>().ObjectInit(type, objState, pos, num, fadeSkip);
        }

        /// <summary>
        /// 获得一个整数概率
        /// </summary>
        static int IntProbability { get { return RandomInt(0, 100); } }
        /// <summary>
        /// 获得一个浮点概率
        /// </summary>
        static float FloatProbability { get { return RandomFloat(0f, 100f); } }
        /// <summary>
        /// 随机int
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        static int RandomInt(int min, int max)
        {
            return Random.Range(min, max);
        }
        /// <summary>
        /// 随机float
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        static float RandomFloat(float min, float max)
        {
            return Random.Range(min, max);
        }

        #endregion
    }
}

/// <summary>
/// 自定义特性
/// </summary>
class CustomCharInfo
{
    /// <summary>
    /// 状态
    /// </summary>
    public C_Buff c_Buff;

    /// <summary>
    /// 键名
    /// </summary>
    public string name;

    /// <summary>
    /// 中文名
    /// </summary>
    public string t_name;

    /// <summary>
    /// 值1
    /// </summary>
    public float value1;

    /// <summary>
    /// 值2
    /// </summary>
    public float value2;

    /// <summary>
    /// 描述
    /// </summary>
    public string description;

    /// <summary>
    /// 图标地址
    /// </summary>
    public string iconAddress;

    /// <summary>
    /// 图标键名
    /// </summary>
    public string iconKey;

    /// <summary>
    /// 使用的单位
    /// </summary>
    T_Citizen user;
    public T_Citizen User
    {
        get { return user; }
        set
        {
            user = value;

            ModLog.Debug($"{user.m_UnitName} 启用了特性 {t_name}");
        }
    }

    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool IsActive { get { return user != null; } }

    public void ClearUser()
    {
        user = null;
    }

    public CustomCharInfo(C_Buff c_Buff)
    {
        this.c_Buff = c_Buff;

        name = "";
        t_name = "";
        iconKey = "";
        user = null;
    }
}
