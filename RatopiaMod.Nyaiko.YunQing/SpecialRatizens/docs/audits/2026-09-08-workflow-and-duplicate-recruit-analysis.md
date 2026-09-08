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
- 关闭整合版中无关的设置窗口、共享仓库、AI、乌托邦等功能。

代码分层：

| 层 | 文件 | 职责 |
| --- | --- | --- |
| 入口 | `Plugin.cs` | BepInEx 插件、配置 `General.Enabled`、Harmony 安装/回滚、故障隔离边界 |
| 数据 | `Core/SpecialDataCatalog.cs`、`Core/CsvTable.cs` | CSV 全量校验加载（表头、枚举、概率范围、特性唯一归属、图标文件存在性），失败整体拒绝 |
| 选择引擎 | `Core/SpecialSelectionEngine.cs`、`Core/SpecialRegistry.cs` | 纯逻辑：概率抽取、保底阈值 10000、繁荣度加成、`IsUsed` 去重、保底值写回 |
| 策略 | `Core/SpecialNamePolicy.cs`、`Core/ProsperityBaselinePolicy.cs`、`Core/SkinRepairPolicy.cs`、`Core/CustomIconKeys.cs`、`Core/PluginDataPaths.cs` | 名字规范化（去颜色标签）、秦律繁荣基线幂等、皮肤修复回退、图标双键、`DLL 同级/Data` 路径解析 |
| 补丁 | `Patching/PatchRegistry.cs`、`PatchDescriptor.cs`、`LegacyPatchAdapters.cs`、`SessionPatches.cs` | 39 个白名单 Harmony 补丁，逐个安装，任一失败整体回滚；执行期异常回退原版行为 |
| 遗留核心 | `Legacy/CustomMOD.cs`（约 1.1 万行）及 `Legacy/` 其余 | 实际业务：会话状态、候选生成、招募登记、24 特性公式、图标、皮肤 |

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

| 层 | 位置 | 内容 | 失效条件 |
| --- | --- | --- | --- |
| 1 | `CustomSpecialUnit.isUsed` | 会话内「已拥有」标记，招募/读档识别时置 true，会话重置时清零 | 会话重置（读档收尾）后、重建前的时间窗；或该特殊鼠不在当前市民列表（死亡/丢失） |
| 2 | `usedNames` | 当前存档全体市民规范化名字；候选过滤时 `IsTaken(unit.name, usedNames)` | 只在 `LoadCitizenDatas`（读档收尾）重建；读档中途（列表重建时）为空或陈旧 |
| 3 | `SpecialCitizens.ContainsKey` | `AddSpecialCitizen` 内部防重复登记 | 只阻止「登记」，不阻止「市民本体生成」——第二副本照样进世界 |
| 护栏 | `CCMake_Info` 前缀 2899 行 | 构造候选卡前按 `Citizens` 名字查重 | 读档路径上 `Citizens` 尚未填充（市民在 UnitData 步才生成），护栏形同虚设 |

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
