# 特殊鼠鼠（SpecialRatizens）工作流程与「同一存档重复招募」问题分析

- 日期：2026-09-08
- 分支：`fix/yunqing`
- 代码基线：v0.1.4（迁移至 `RatopiaMod.Nyaiko.YunQing` 解决方案后的 v0.1.5 起始点）
- 分析对象：`RatopiaMod.Nyaiko.YunQing/SpecialRatizens/`（源码）+ 游戏 `Assembly-CSharp.dll` 反编译对照
- 结论先行：重复招募的主因是**读档时序竞态**——读档过程中移民列表的重建（含特殊鼠鼠抽取）发生在「存档市民生成」和「mod 会话状态重建」之前，去重状态尚未反映当前存档，已拥有的特殊鼠鼠有机会被再次抽成候选卡；招募这张卡后，第二副本绕过全部登记进入存档。特殊鼠死亡后重读档重新可抽是另一个（可能是有意的）缺口。

---

## 1. 插件定位与结构

特殊鼠鼠是从整合版 RatopiaMod 1.5.x 独立迁移出的 BepInEx 5 插件：

- 12 名特殊鼠鼠（`Data/CustomSpecialUnit.csv`），24 个自定义特性（`Data/CustomCharInfo.csv`），24 张特性图标（`Data/Icon/*.png`）。
- 保留原刷新概率（万分比）、保底、繁荣度概率加成、三维/金币、外观（Spine 皮肤组合）与特性效果（战斗、幸福度、贸易、工作、电力、机器人）。

## 2. 启动与数据注册流程

1. **`Plugin.Awake`**
   - 读取配置 `General.Enabled`（关闭时仍注册特性定义，保证旧存档可读）。
   - `PluginDataPaths.ResolveDataRoot`：数据根 = 插件 DLL 同级目录下的 `Data`（因此 `Data/` 必须随构建输出，v0.1.5 已以 `Content Include` 方式随构建拷贝）。
   - `SpecialDataCatalog.Load`：两张 CSV 预检（校验失败抛异常 → `Awake` 捕获 → `UnpatchSelf` + 会话重置，插件停用）。
   - `CustomMOD.ConfigureSpecialRatizens`：写入数据路径、把 `Enabled` 写入 `CustomSettings.CustomSpecialUnit`。
   - `PatchRegistry.InstallAll`：39 个补丁逐个安装。

2. **`DB_Mgr.Awake`（游戏侧，每次进程启动一次）→ 补丁 `data.character-db`**
   - Postfix `CustomMOD.DB_Mgr_Character_DB_Setting`：
     - 快照原版特性表长度 `DefaultChar1Count/DefaultChar2Count`（后续生成普通候选时只在前 N 个原版特性里抽，防止普通鼠抽到自定义特性）；
     - `LoadCustomDatas()`：重新解析两张 CSV，构建 `CustomSpecialUnitDatas`（12 只，`isUsed=false`、`pdr_C=0`）、`CustomSpecialUnitRandomGroup`（按 grade 分组的保底重置组）；
     - `RegisterCustomCharInfo` → `UpdateCharDB`：把 24 个自定义特性**追加**进原版 `List_Char1_DB/List_Char2_DB`，`Index = 末位+1`；已存在同名特性则复用 Index、仅刷新显示文本（对应 ScriptableObject 读档丢汉字的问题）——**Index 绑定按名称幂等**；
     - 图标注册：以 `SpecialRatizens.Icon.<特性名>` 为主键写入 `Func.Dic_Resource`，并对游戏写死的 `Icon_Char{Index}` 写兼容别名；重复读档幂等更新；
     - `LoadProsperityDB`：深复制繁荣表基线（商鞅「秦律」幂等计算的基准）。

## 3. 会话生命周期（读档重建）

- 补丁 `session.loaded`：`TileMgr.All_NotUseListClear` 的 Postfix → `SessionPatches.TileMgrAllNotUseListClearPostfix` → `CustomMOD.SpecialRatizensSessionLoaded`：
  1. `ResetSpecialRatizensSession()`：清空特殊市民表、皮肤缓存、`specialUnit` 待选对象；**把 12 只 `isUsed` 全部置回 false、`pdr_C=0`**；清空 24 个特性使用者。
  2. `LoadCitizenDatas()`：遍历当前 `Citizens`（= `T_UnitMgr.List_Citizen`）：
     - 重建 `usedNames`（全体市民规范化名字，供普通命名避让与特殊鼠去重）；
     - **按特性匹配识别特殊市民**（`TryGetSpecialUnit(citizen)`：市民任一特性名 == 某特殊鼠的 char1/char2），命中则 `AddSpecialCitizen`：强制名字回 CSV 名、`SpecialCitizens[name]=citizen`、**`isUsed=true`**、重建 Spine 皮肤、登记两个特性的使用者、三维下限恢复。
  3. `UpdateAllUsedSpecialEffects()`：按登记结果重建全局/单体特性效果（贸易商业值、电网、奥米伽 PDI 等）。

- 游戏侧读档时序（反编译 `TileMgr.<MakeMapC>d__41.MoveNext` 与 `LoadMgr.LoadSettingC` 证实）：

  ```
  MakeMapC 协程（进世界，含新游戏与读档）
    ├─ 地形/建筑铺设 …
    ├─ UnitSpawning()
    ├─ IsLoadGame → _LoadMgr.LoadSetting()（LoadSettingC 协程）：
    │    1. DBMgr（读档数据库）
    │    2. Prosperity …
    │    3. SysMgr  → SystemMgr.LoadSetting()
    │         └─ 若存档 m_CitizenCaveProductFinish==true：
    │              GameMgr._CCUI.MakeCitizenList()   ← ★ 移民列表在此重建
    │    4. …
    │    5. UnitData → T_UnitMgr.LoadSettings()      ← ★ 存档市民此时才生成
    │    6. 建筑/电网/事件 …
    └─ All_NotUseListClear()                          ← ★ mod 会话重建在此（最后）
  ```

## 4. 特殊鼠鼠生成与招募流程（核心玩法链路）

1. **列表重建触发点**（反编译证实共三处，均调用 `CitizenCaveUI.MakeCitizenList`）：
   - `SystemMgr.LoadSetting`：读档时若存档标记 `m_CitizenCaveProductFinish==true`；
   - `SystemMgr.Update`：鼠洞周期计时器到期（`m_CitizenCaveCurTime >= GetMaxTime()` 且建筑完工），重建列表并恢复刷新次数 `m_RestRerollCount`；
   - `CitizenCaveUI.RerollBtn`：玩家手动刷新按钮。

2. **抽取（`CitizenCaveUI_MakeCitizenList` 前缀，CustomMOD.cs 2858）**
   - 以 12 只 `CustomSpecialUnit` 构造 `SpecialCandidateState`，每只标记 `IsUsed = unit.isUsed || SpecialNamePolicy.IsTaken(unit.name, usedNames)`；
   - `SpecialSelectionEngine.Select`：
     - 候选池 = 未使用项；若有 `RealProbability = probability + pdr_C ≥ 10000`（保底线）直接选中基础概率最低者；
     - 否则按顺序逐个掷点：`UnityEngine.Random.Range(0,10000) < probability × (1 + 繁荣等级×5%)`，命中即选；
     - 每轮给所有未使用候选 `pdr_C += 1`（保底累积）并写回 `unit.pdr_C`；
   - 命中者存入静态 `specialUnit`（待使用，一次性），日志输出「出现特殊单位 …」。

3. **候选卡构造（`CCMake_Info..ctor(int,bool)` 前缀）**
   - 若 `specialUnit != null`：先做最后护栏——特殊鼠名字若已存在于当前 `Citizens` 名单，放弃本次特殊候选；否则用特殊鼠数据构造候选卡（名字含颜色标签、性别、三维、金币、两个特性 Index、预览皮肤），返回 false 跳过原构造；`specialUnit` 消费置空。
   - 未命中特殊时：若设置了移民性别限制，mod 代为构造；否则走原版。`CCMake_Info.MakeCharacterList` 前缀把特性抽取限制在原版前 N 个特性内（`DefaultChar1Count/DefaultChar2Count` 边界），普通候选不可能抽到自定义特性。

4. **招募落地（`T_Citizen.MakeCtizen_ByCC(Vector2, CCMake_Info)` 后缀）**
   - 原版行为：建市民、写特性、算三维、设置 `m_UnitName = _info.Name`；**若已有同名市民，原版自动追加 `_NNN` 后缀改名**；从随机名池删除该名。
   - mod 后缀：
     - `TryGetSpecialUnit(citizen)`（按特性匹配）识别特殊鼠 → `AddSpecialCitizen`：强制 `m_UnitName = unit.Name`（覆盖原版的 `_NNN` 改名）、登记 `SpecialCitizens`（已存在同名键则告警「已存在」并返回 false）、`isUsed=true`、加入 `usedNames`、同 grade 组 `pdr_C` 清零、注册正式皮肤；
     - 遍历候选两个特性，命中群体效果立即全图应用（奈奈的仁爱、联邦的哀伤/希望、秦律、岳家军、五禽戏、梨园、量子电网、量子机械）；
     - 再次 `UpdateCitizenUsedSpecialEffects`，保证新招募者自身效果无需重读档即生效。

## 5. 防重复招募机制现状（三层 + 一道护栏）

| 层   | 位置                          | 内容                                                                   | 失效条件                                                                        |
| ---- | ----------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| 1    | `CustomSpecialUnit.isUsed`    | 会话内「已拥有」标记，招募/读档识别时置 true，会话重置时清零           | 会话重置（读档收尾）后、重建前的时间窗；或该特殊鼠不在当前市民列表（死亡/丢失） |
| 2    | `usedNames`                   | 当前存档全体市民规范化名字；候选过滤时 `IsTaken(unit.name, usedNames)` | 只在 `LoadCitizenDatas`（读档收尾）重建；读档中途（列表重建时）为空或陈旧       |
| 3    | `SpecialCitizens.ContainsKey` | `AddSpecialCitizen` 内部防重复登记                                     | 只阻止「登记」，不阻止「市民本体生成」——第二副本照样进世界                      |
| 护栏 | `CCMake_Info` 前缀 2899 行    | 构造候选卡前按 `Citizens` 名字查重                                     | 读档路径上 `Citizens` 尚未填充（市民在 UnitData 步才生成），护栏形同虚设        |

## 6. 重复招募根因分析

### 6.1 主因：读档时序竞态（证据链完整）

1. `SystemMgr.LoadSetting`（LoadSettingC 第 3 步）中，若存档 `m_CitizenCaveProductFinish==true`，游戏调用 `MakeCitizenList()` 重建移民列表（反编译 SystemMgr.LoadSetting 证实）。
2. 此时（第 3 步）存档市民尚未生成（第 5 步 UnitData 才执行），mod 的会话重建更未运行（在 MakeMapC 收尾的 `All_NotUseListClear`，即 LoadSettingC 整体结束后）。
3. 因此抽取前缀运行时：`isUsed` 全部为 false（进程启动时 `LoadCustomDatas` 重置；跨存档切换时则是上一个世界的陈旧值），`usedNames` 为空/陈旧 → **已存在于该存档的特殊鼠鼠是可抽状态**。
4. 抽取按概率掷点（如奈奈酱 300/10000，再乘繁荣加成，12 只合计单次读档数个百分点量级），命中即生成该特殊鼠的候选卡；`CCMake_Info` 名字护栏因 `Citizens` 为空而放行。
5. 读档收尾，会话重建把存档里的原版特殊鼠正式登记（`isUsed=true`），但**先前生成的那张候选卡仍留在移民列表里**。
6. 玩家招募这张卡 → 原版生成第二只同名市民（先被原版改名为 `名字_NNN`）→ mod 后缀 `AddSpecialCitizen` 强制把名字改回原名 → `SpecialCitizens.ContainsKey` 命中「已存在」仅跳过登记 → **第二副本以完全相同的名字进入存档**。
7. 由于招募路径**忽略 `AddSpecialCitizen` 返回值**（3075 行），第二副本还会：把同 grade 保底清零、重复执行群体效果（秦律/岳家军/五禽戏等再叠加一轮）、并**抢占特性使用者登记**（`customInfo.User` 指向第二副本）。
8. 该重复永久固化在存档：下次读档 `LoadCitizenDatas` 登记先匹配到的一只，第二只走「已存在」分支被跳过（无正式皮肤、无效果、无使用者登记）。

这与用户反馈「**同一个存档内可能会**招同样的特殊鼠鼠」的偶发特征吻合：是否复现取决于读档时那次随机抽取是否命中存档内已有的特殊鼠（概率 + 繁荣加成 + 是否 `m_CitizenCaveProductFinish==true`），并非每次必现。

### 6.2 次因（独立缺口，需产品决策）

- **特殊鼠死亡/丢失后重读档可再抽**：`isUsed` 不持久化、会话重建只认当前市民，死亡的特殊鼠在下次读档后回到候选池。若设计上允许「再获得」，则无碍；若不允许，需要把「已消耗」持久化（或至少与原版特殊单位的表现对齐）。
- **原版 `_NNN` 改名被覆盖**：`AddSpecialCitizen` 无条件 `m_UnitName = unit.Name`，抹掉原版的重名消解，使重复副本与原体显示完全同名，放大问题观感。
- **跨存档切换窗口**：进程内切换存档时，候选抽取用的是上一个世界的 `isUsed/usedNames`（新世界的重建在收尾才发生），可能错误排除/放行个别特殊鼠。

### 6.3 修复方向建议（本次不实施）

1. **选择时实时判定（推荐的不变量）**：抽取前缀里不依赖缓存的 `isUsed/usedNames`，改为「当前 `Citizens` 中存在携带该特殊鼠 char1/char2 任一特性的市民 → 不可抽」。按特性匹配（`TryGetSpecialUnit` 已有同款逻辑）比按名字更稳，天然覆盖读档任意阶段（只要市民已生成）、改名、死亡策略也可单独控制。
2. **把会话重建提前**：在读档早期（如 `SystemMgr.LoadSetting` 前缀或 `T_UnitMgr.LoadSettings` 后缀）先做一次「按存档市民重建 isUsed/usedNames」，使 `MakeCitizenList`（第 3 步）运行时状态已正确；收尾的全量重建仍保留。
3. **招募路径尊重登记结果**：`AddSpecialCitizen` 返回 false 时直接 return，不再清保底、不再重复应用群体效果、不抢占特性使用者；并考虑给第二副本保留原版 `_NNN` 改名或直接拒绝生成。
4. **死亡策略显式化**：决定死亡后是否重新可抽，并写进文档/测试合同。

## 7. 其余 38 个补丁的功能分组（概览）

- 电力（8）：机器人创建/疲劳、接线、瓦特、四种 WireCheck、四向电网、删接检查、量子电网耗电。
- 产业（4）：石作工作前后缀、食物/生命恢复、旅店容量。
- 经济（5）：进出口价格、贸易结果通知、地形距离、贸易协定上限、贸易明细。
- 市民/战斗（3）：JobSet、挥砍攻击、受击减伤（猎人等）。
- 状态/图标（7）：食物总量、PDI 经验、饥饿、Buff 图标、图标地址、显示名、描述（支撑 24 个自定义特性在 UI 的正确呈现）。
- 外观（2）：默认服装、工作服（配合按性别重建特殊模板、四部件校验、失败回退原皮肤）。

## 8. 迁移备注（v0.1.5 起始状态）

- 源码与 `Data/`（2 张 CSV + 24 张图标）已从 `SpecialRatizens/src`、`SpecialRatizens/Data` 迁至 `RatopiaMod.Nyaiko.YunQing/SpecialRatizens/`；命名空间、程序集名、BepInEx GUID（`cn.ratopia.specialratizens`）保持不变，保证配置与旧存档兼容。
- 版本统一为 0.1.5（csproj `Version/AssemblyVersion/FileVersion` + `Plugin.PluginVersion`）。
- csproj 新增：`Data\**\*` 随构建输出（满足 `ResolveDataRoot` 的「DLL 同级 Data」约定）；`InstallSpecialRatizensData` 目标在 Release 构建的共享安装步骤后把 `Data/` 一并拷入 `BepInEx/plugins/SpecialRatizens/`，避免只装 DLL 的半安装状态。
- 旧目录 `SpecialRatizens/`（含测试项目与其 sln）保持原样未动，作为对照基线。

---

## 9. 附录：v0.1.5 第二步重构记录（数据与配置 JSON 化）

> 本文第 1–8 节撰写于重构前，其中「CSV」的描述自本节起由 JSON 取代；流程与行为语义不变。

- **数据文件 JSON 化**（`Data/` 目录，随构建输出）：
  - `CustomSpecialUnit.json`：12 名特殊鼠鼠，字段名与原 CSV 列名一致，数组顺序即注册顺序（特性 Index 绑定依赖顺序，禁止重排）；
  - `CustomCharInfo.json`：24 个自定义特性；
  - `Names.json`：原 CustomMOD 内置的姓名表（姓氏 508 / 女名 89+1377 / 男名 136+371），由 `Core/NameTableStore` 惰性加载（首次访问才读文件），缺失或损坏时降级为空表。
- **加载与校验**：`Core/SpecialDataCatalog.Load(dataRoot)` 用 Newtonsoft.Json 直接反序列化为运行时对象（`CharacterInfo` / `CustomSpecialUnit`），并保留原有全量校验（字段缺失、名称重复、Category 0/1、概率 0–10000、特性引用、特性唯一归属、图标文件存在性），失败抛 `InvalidDataException` 整体拒绝；`Legacy/CsvTable.cs` 与 BaseCommand 的 CSV 解析在数据路径上退役。已通过「游戏程序集 + 编译产物」的真实加载冒烟测试（12/24 全量绑定、损坏 JSON、缺字段均按预期报错）。
- **配置 BepInEx 化**：废弃自管 `CustomSettings.json`（`LoadCustomSettings/SaveCustomSettings` 已删除）。活跃设置改由 `Configuration/ModConfig` 以 `ConfigEntry<T>` 承载，节/键与旧 `General.Enabled` 兼容：
  - `General.Enabled`（原 CustomSpecialUnit）；
  - `Generation.OnlyGoodCharacteristic`（移民候选全正面特性）；
  - `Generation.NewCitizenGenderLimit`（-1 不限制 / 0 男 / 1 女）。
    整合版遗留但独立版未安装补丁的 29 个设置开关收进 `CustomMOD.LegacySettings`（静态默认值，仅供遗留代码路径读取，不再序列化为任何文件）。
- **路径标准化**：`Core/PluginDataPaths` 改用 BepInEx `Paths.PluginPath` + `System.IO.Path.Combine`（`BepInEx/plugins/SpecialRatizens/Data`）；`CustomMOD.CustomDataPath` 不再保存尾部分隔符，全部路径拼接走 `Path.Combine`。
- **CSV 彻底清退**（第二步重构补遗）：
  - **敌方死亡掉落功能整体移除**（`CustomEnemyDrop` / `EnemyDropDatas` / `LoadEnemyDropDatas` / `GameEnemy_DeathCheck` / `EliteEnemy_DeathCheck`）：这是整合版的遗留功能——通过 `CustomEnemyDrop.csv`（列：Name=敌人类型、T_Name=显示名、DropList=掉落物列表）为每种敌人配置自定义死亡掉落，开关 `EnemyDeadthDrop` 开启后在敌人死亡位置按概率生成物品。独立版从未安装对应的死亡补丁、开关默认关闭、且 Data 目录里从来没有这个 CSV，属于彻底死代码。
  - **游戏数据库 CSV 导出脚手架移除**（`OutPutCSVDatas` 及 `OutPutGameDatas` 中 40 处调用）：整合版开发期把原版游戏 40 张数据库表导出为 CSV 的调试工具，与特殊鼠鼠功能无关；`OutPutGameDatas` 保留 JSON 导出（SkinBunlde）。
  - **BaseCommand 精简**（714 → 约 280 行）：删除 `LoadCsvData` 全家族（泛型/字典/反射映射/文本解析）、两个 `SaveCsvData`、`ReplaceCsvText`、`ObjectToCsvText`、`CsvTextToObject`、`SaveFileData` 及仅被 CSV 链使用的 `ObjectToJson`/`JsonToObject`；保留仍被皮肤存档与 JSON 导出使用的 `SaveObjectToJson`/`LoadObjectByJson`/`SaveFile`，并修正 `LoadObjectByJson` 上误标的「CSV文本转类」注释。`Legacy/CsvData.cs`、`Legacy/CustomEnemyDrop.cs` 两个文件删除。
  - 至此**所有 .cs 文件零 CSV 引用**，数据面仅存 JSON（`Data/*.json`）与 PNG（`Data/Icon/`）。

## 10. 附录：重复招募修复实现（D_Data.ModsData 持久化 + 读档时序修复）

> 落实第 6.3 节的修复方向 (b)（提前恢复时序）与 (e)（持久化状态）；(c)（招募路径尊重返回值）由既有的 `CCMake_Info` 名称守卫（跳过重复名候选）覆盖，时序修复后该路径不可达。

### 10.1 存档容器：D_Data.ModsData

- `D_Data.ModsData` 是游戏存档自带的 `Utility.Savable.SavableData`——通用命名键值存储（`AddData(key, value)` / `GetValue<T>(key, default)` / `HasKey(key)` / `Create()`），随 D_Data 一起序列化进存档、随存档复制分享。
- 游戏自身仅用 `"Mods"` 键记录存档时的模组清单（`PlayDataMgr.Save()` 最后一步 `SetMods`）。本插件新增键 `"SpecialRatizens"`，载荷为 JSON 字符串：`{"version":1,"pity":{"鼠名":pdr_C}}`。
- 关键时序：读档协程在异步反序列化完成后调 `PlayDataMgr.LoadData(D_Data)`，**早于** `LoadSettingC` 的全部步骤——这是把状态恢复插到「洞列表重建（SysMgr 步骤）」之前的唯一干净窗口。

### 10.2 两个新补丁

- **`save.data-loaded`**：`PlayDataMgr.LoadData` Postfix → `CustomMOD.PlayDataMgr_LoadData`：
  - 读取 ModsData 中的 `pity`，直接恢复各鼠鼠 `pdr_C`（损坏载荷告警并按零处理）；
  - 按**存档数据本身**推导已消耗集合（`D_Data.List_Citizen` 存活 ∪ `D_Data.List_DeathCitizen` 遗体，按名字归一化匹配），把对应鼠鼠 `isUsed = true`——此后 SysMgr 步骤的洞列表重建自然排除它们，**读档竞态从根上消除**。
- **`save.mods-set`**：`PlayDataMgr.SetMods` Postfix → `CustomMOD.PlayDataMgr_SetMods`：`SetMods` 是 `PlayDataMgr.Save()` 的最后一步、zip 序列化之前，此处把当前全部非零 `pdr_C` 写入 ModsData，随存档持久化。

### 10.3 会话重建增补（`LoadCitizenDatas`）

- 原逻辑只按特性匹配存活市民重建 `isUsed`，会把「有遗体」的鼠鼠重新开放招募。增补：遍历 `GameData.List_DeathCitizen`，遗体尚在的特殊鼠鼠同样标记 `isUsed` 并把名字占进 `usedNames`（防止普通市民占用名字导致遗体消失后永久无法再次招募）。

### 10.4 行为语义与兼容性

- **死亡策略（用户选定）**：死亡后可再次招募——以「遗体是否仍在死亡名单」为准；遗体被游戏移除后该鼠鼠重新进入候选池，且保底计数延续存档值。
- **保底持久化（用户选定）**：`pdr_C` 随存档保存/恢复，读档不再清零（原 `AddSpecialCitizen` 在会话重建时把已拥有鼠鼠保底清零的行为保留——招募时清零本就是设计语义，且已拥有鼠鼠存档值恒为 0）。
- **老存档兼容**：无 `"SpecialRatizens"` 键 → 保底按零、已消耗由存档名单推导，行为与修复前一致（除竞态消除外无变化）。
- **历史受损存档**：此前因 bug 产生的同名双份特殊市民，名字匹配会把两个都标记为已拥有，不会误开放。
- 新游戏不经过 `LoadData`，会话状态由既有 `session.loaded` 补丁（`ResetSpecialRatizensSession`）自愈，无需额外钩子。

### 10.5 构建与验证

- `SpecialRatizens.csproj` 新增引用 `Utility.Savable.SavableData.dll`（游戏 Managed 目录，Private=false）。
- Debug 构建 0 警告 0 错误（补丁总数 39 → 41）。
- 冒烟测试（真实游戏程序集 + 编译产物）：`PlayDataMgr.LoadData(D_Data)` / `SetMods(string[])` 目标唯一且签名匹配；适配器与处理器按 Harmony 注入约定对齐；游戏真实 `SavableData` 类型 `Create/AddData/HasKey/GetValue<string>` 中文键值往返一致；`ModsSavePayload`（产物内私有类型）Newtonsoft 序列化/反序列化闭环一致。

## 11. 附录：最激进裁剪（只留特殊鼠鼠，数据只存 ModsData 与 ConfigFile）

### 11.1 裁剪原则（用户指令）

- 除特殊鼠鼠外的全部功能一律删除；不再保留整合版遗留的任何设置窗口、快捷键、场景钩子、存档 side-car 文件。
- 数据只允许存在于两处：存档内 `D_Data.ModsData`（键 `"SpecialRatizens"`）与 BepInEx `ConfigFile`（仅 `General.Enabled`）。
- 皮肤系统经确认**保留专属外观**（`RegisterCustomSkin` / `SpecialCitizenSkins` / `UpdateUnitSpineDress` 链路 + JSON 皮肤字段），但删除玩家自定义市民皮肤编辑器。

### 11.2 判定关键：哪些"系统类"补丁其实是特性效果基建

逐个核读效果区实现后确认：`power.*`（奥米伽-7 量子电网/机械供电）、`industry.*`（皮卡丘鼠力发电站、大正蘑菇农场、李隆基访客数）、`economy.*`（白圭贸易价格、王亥距离与协议数）、`citizen.job`（岳家军）、`combat.sword-attack` / `combat.citizen-attacked`（岳家枪/七探蛇盘枪/神医在世/奥米伽免死）、`state.*`（联邦的希望产金、状态图标/名称/描述、PDI 缓存）全部是 24 特性公式的载体，**全部保留**。`appearance.*` 两个服装补丁服务特殊鼠鼠专属外观，保留。

### 11.3 删除清单

- `CustomMOD.cs` 由 10712 行裁至 3345 行：删除名称表、LegacySettings（29 个死开关）、自定义存档设置（`.set` side-car + `SystemMgr_SystemPause` / `PlayDataMgr_Save(_Post)` / `SaveLoadMgr_SaveAsync_Zip`）、开始游戏界面（地图种子）、IMGUI 设置界面（约 35 个按钮）、操作监听、自定义快捷键、跳转场景、`TileMgr_All_NotUseListClear` 旧处理器、DB 初始化遗留（`DB_Mgr_Awake` / `Res_DB_Setting` / `Build_DB_Setting`、`OutPutGameDatas` / `OutPutJsonData`、好特征/仓库/物品价格表加载）、全正面特征、更多负重/经验/猎手减伤/连续拾取/默认选中/建造无需材料/友方掉落无伤/共享床位/贸易消息/贸易详细/乌托邦/和平模式/无限人口/输入框限制/移动路径/地图编辑/共享仓库/仓库直供/更多名称/AI 相关（含 `CitizenDesireThreshold`）/饭桌优化/寻路相关/女王建造与女王 Update 解析/无人机翻倍/机器人翻倍/Defines 反射工具/测试用，以及玩家皮肤编辑器（`CitizenInfoUI_Show/Hide`、`CitizenCustomSkins`、`Save/LoadCustomSkinSetting`）。
- 补丁 41 → 40：删除 `economy.detail-price`（贸易详细信息，唯一服务于已删功能的补丁）；其余 40 个均为特殊鼠鼠本体或特性效果。
- 文件删除：`Legacy/Settings.cs`（`GameSaveCustomSettings` side-car 数据类）、`Core/NameTableStore.cs`（更多姓名）、`Data/Names.json`。
- `ModConfig` 裁至 `General.Enabled` 一项；`CustomMOD.ActiveCustomSpecialUnit` 改为只读直读配置（原 setter 的效果刷新副作用由 `SpecialRatizensSessionLoaded` / 事件补丁承担）。
- `CCMake_Info` 删除移民性别限制分支；`MakeCharacterList` 保留（`DefaultChar1Count/DefaultChar2Count` 上限仍是"自定义特性只归属特殊鼠鼠"的护栏），仅删全正面特征分支；`T_Citizen_BeAttacked` 删友方掉落无伤分支（保留神医在世 + 奥米伽免死）。
- `BaseCommand` 裁至图标加载（`LoadSprite` / `LoadSpriteFromTexture2D` / `LoadTextureFromFile` / `LoadFile`）与枚举转换（`StringToEnum` 等），删除 JSON 存取/反射工具（皮肤编辑器专用）。

### 11.4 保留清单（功能面）

12 鼠鼠 JSON 数据与图标、24 特性注册与图标、洞穴候选选择（保底 `pdr_C` 持久化 + `isUsed` 去重 + 名字占用护栏）、招募强制命名与三维/金币覆盖、死亡后遗体名单判定可再招募、24 特性效果（含奥米伽-7 量子电网全链路）、专属皮肤注册与 Spine 重建（含职业皮肤、修复回退）、`D_Data.ModsData` 持久化与老存档无键兼容、`session.loaded` 会话重建。

### 11.5 构建与验证（本轮）

- Debug 构建 0 警告 0 错误；region 12/12 平衡、大括号 556/556 平衡。
- 全项目残留扫描：`LegacySettings` / `NewCitizenGenderLimit` / `NameTableStore` / `GameSaveCustomSet` / `CitizenCustomSkins` / `Defines*` / `UtopiaMode` 等 60+ 个已删符号 0 命中。
- 反射冒烟测试（真实游戏程序集 + BepInEx core + 编译产物）：37 个保留方法、9 个核心字段全部存在；17 个代表性遗留方法确认从产物中消失；`ModConfig` 仅剩 `Enabled`。

### 11.6 修订：恢复名称表与「更多姓名」（用户要求）

- 恢复 `Core/NameTableStore.cs` 与 `Data/Names.json`（git 从上一提交还原）；`CustomMOD` 恢复「名称表」region（`NameTables` / `CustomSurNames`）与「更多名称」region（`PerNames_Female/Male`、`tempUsedNames`、三个处理器）。`usedNames` 字段沿用 §10.3 修复时写入数据加载 region 的版本，避免重复定义。
- **接活补丁**：这三个处理器在迁移后从未注册 Harmony 补丁（`ActiveCustomNames` 读 LegacySettings 默认 false），本次按标准模式补齐——
  - `names.random-name`（prefix）→ `CitizenCaveUI.GetRandomName(Gender)`：用 `Data/Names.json` 生成中文姓名，避开 `usedNames` 与本批次 `tempUsedNames`；
  - `names.list-reset`（postfix）→ `CitizenCaveUI.MakeCitizenList`：每次生成移民列表清空本批次用名；
  - `names.citizen-recorded`（postfix）→ `T_Citizen.MakeCtizen_ByCC`：普通移民落名后记入 `usedNames`（特殊鼠鼠的名字由 `AddSpecialCitizen` 负责登记，处理器内部对特殊鼠鼠跳过）。
- 开关：`ModConfig` 新增 `General.CustomNames`（默认 `true`），`CustomMOD.ActiveCustomNames` 只读直读配置。
- 补丁总数 40 → 43；Debug 构建 0 警告 0 错误；反射冒烟测试确认三个处理器、`NameTableStore`、`ModConfig.CustomNames` 与适配器全部就位，`Data/Names.json` 随构建复制到输出目录。

### 11.7 修复与日志体系（§11.6 之后）

- **修复读档后特殊特性详情空白**（hotrepl 运行时定位）：游戏读档会原地清空 ScriptableObject 特性库中已注册特性的显示字段（`T_Name`/`Description`/`Icon`，效果数值保留）；JSON 化后 `RuntimeTraits` 缓存对象与 DB 条目同引用，`LoadCustomDatas` 重跑时「已存在分支」回填变成自赋值。修复：每次会话从 `SpecialTraitDefinition`（不可变字符串快照）重建全新 `CharacterInfo`，存量存档读档时自愈。
- **日志体系重构**（对齐 `RatopiaMod.YunQing.All` 的 `ModLog` 风格）：新增 `Core/ModLog.cs`（`Initialize(ManualLogSource)` + Debug/Info/Warn/Error），插件 `Awake` 注入。全部 95 个散落的 `Unity Debug.Log*` 调用收敛为 69 个分级调用点并删除 14 条注释死日志：
  - **Info（15，关键事件）**：插件启动与补丁安装汇总、特性/单位数据加载与逐单位注册、读档识别特殊鼠鼠与会话恢复完成、招募获得/本体创建、遗体判定、量子电网启动/合并、十万伏特触发、贸易完成统计；
  - **Debug（29，默认不可见）**：高频运行时细节（电网逐台连接与统计、每笔贸易价格、战斗击退、换装/皮肤细节、候选概率、护栏触发、图片加载）；
  - **Warn（17，异常与防护）**：配置错误、存档解析失败、防重复触发、发电失败、皮肤部位异常、数据不一致；**Error（8）**：皮肤组合失败、补丁执行失败、登记异常。
  - 删除纯噪音：幸福度逐 tick、十万伏特检查布尔、电网重建 8 行统计块。
