# Ratopia Mod Collection

这是一个《鼠托邦（Ratopia）》BepInEx 5 Mono Mod 的源码集合仓库。仓库中的每个目录都是一个可以独立安装、独立发布、独立卸载的 Mod；Release 中的 `Ratopia-Mod-<时间戳>.zip` 则是把当前仓库内全部最新 Mod 打在一起的集合包。

当前集合不包含游戏本体、Unity、BepInEx 或 Harmony 的 DLL。游戏相关引用 DLL 只保存在私有依赖仓库中，供 GitHub Actions 构建使用。

## 下载与安装

GitHub Release 提供两类包：

- **单个 Mod 包**：例如 `RatopiaMod.YunQing.All-v3.0.0.zip`，只包含一个 Mod。
- **全 Mod 集合包**：文件名为 `Ratopia-Mod-<时间戳>.zip`，时间戳随构建时间自动生成，包含仓库中全部最新 Mod。

安装时将发布包直接解压到 Ratopia 游戏根目录，保持 ZIP 内目录结构不变。单个 Mod 的详细安装、卸载、兼容性和存档说明见对应项目目录下的 `README.md`。

## 开发配置

本地构建依赖环境变量 `RATOPIA_DIR`，它用于定位 Ratopia 的安装目录，例如：

```powershell
$env:RATOPIA_DIR = 'E:\steam\steamapps\common\Ratopia'
```

其余目录会根据它派生，例如：

- `BepInExCoreDir = $env:RATOPIA_DIR\BepInEx\core`
- `GameManagedDir = $env:RATOPIA_DIR\Ratopia_Data\Managed`

如需构建整个仓库中的所有 Mod，并生成单独包与全 Mod 集合包，可执行：

```powershell
.\scripts\build-all-mods.ps1 -OutputDirectory artifacts
```

该脚本构建时显式禁止把 DLL 自动复制进游戏目录，只会在 `artifacts` 目录中生成发布产物。如需构建单个 Mod，请进入对应项目目录查看其 README；多数项目可直接执行：

```powershell
dotnet build -c Release -p:DisableRatopiaModInstall=true
```

游戏更新后，可以双击 `scripts\update-ratopia-ci-deps.cmd` 检查 `Assembly-CSharp.dll` 的 SHA-256。哈希变化时会重新打包并上传私有 CI 依赖仓库；哈希不变时不会重复上传。

## 项目集合中的 Mod

### 广播站信号覆盖全图（BroadcastStationGlobalCoverage）

让地图任意位置的电视都能发现并使用正在工作的广播站。Mod 只接管电视手动选台和自动选择信号源的入口，不扩大电视服务附近居民的距离，也不修改广播站电路范围或建筑范围字段。详细说明见 [BroadcastStationGlobalCoverage/README.md](BroadcastStationGlobalCoverage/README.md)。

### 装备重铸闪避属性（EquipmentReforgeDodge）

为装备重铸系统新增“闪避率”候选属性。一阶重铸提供 `+20%` 闪避，二阶重铸提供 `+30%` 闪避；只有饰品会出现该候选，武器与装甲保持原版。Mod 还会让女王装备上的闪避重铸值真正参与闪避计算，并保留原版 90% 闪避上限。详细说明见 [EquipmentReforgeDodge/README.md](EquipmentReforgeDodge/README.md)。

### 装备重铸自选属性（EquipmentReforgeSelector）

在皇家铁匠铺和地狱铁砧的装备重铸界面中列出当前装备可用的原版属性，允许玩家指定本次重铸要获得的属性，减少反复随机重铸的成本。详细说明见 [EquipmentReforgeSelector/README.md](EquipmentReforgeSelector/README.md)。

### 处刑台（ExecutionPlatform）

复用原版监狱的图标、蓝图、模型和动画，新增“处刑台”建筑。玩家可通过标准岗位界面指定一名普通鼠民；目标到达工作位后播放原版监狱动作，随后进入原版死亡流程。女王、儿童、机器人、已受伤、被监禁、已死亡或远征中的单位不会成为目标。详细说明见 [ExecutionPlatform/README.md](ExecutionPlatform/README.md)。

### 上帝视角管理（GodViewManagement）

开启后，女王无需走到建筑旁边即可用鼠标左键直接打开全图已建成建筑的原版配置面板。WASD、方向键和手柄方向输入不会驱动女王移动，但相机移动、屏幕边缘滚屏和其他原版视角操作保持不变。详细说明见 [GodViewManagement/README.md](GodViewManagement/README.md)。

### 加热器加强优化（HeaterEnhancement）

调整原版加热器的供暖范围与季节工作规则。供暖范围扩展为横向 9 格、向下 5 行，一台加热器最多影响 39 格植物；墙体阻挡、耗电、电线连接和原版 HeatSystem 继续生效。加热器只在冬季工作，春、夏、秋会停止耗电并清理无效供暖状态。详细说明见 [HeaterEnhancement/README.md](HeaterEnhancement/README.md)。

### 多用途研究点（MultipurposeResearch）

让完成科技研究后剩余的研究点继续作为额外资源使用。玩家可以在原版研究界面中把研究点用于学术出口、全民动员和女王进修：向国家出售研究点、短期提升全体普通鼠民工作效率，或永久提升女王力量、智慧与敏捷。详细说明见 [MultipurposeResearch/README.md](MultipurposeResearch/README.md)。

### 人口自定义（PopulationCustomizer）

在鼠民名单中新增“上限”按钮，可以为每个存档分别设置鼠民与机器鼠数量上限，范围 `0–999`。设置立即影响招募和制造判定，并随该存档一起保存；超过新上限的现有单位不会被删除。详细说明见 [PopulationCustomizer/README.md](PopulationCustomizer/README.md)。

### 云清整合（RatopiaMod.YunQing.All）

云清 Mod 的 BepInEx 5 独立维护版本，包含调试控制台、地形控制台、控制面板、汇率券生成、银行兑换倍率、鱼溺水等一批长期维护的功能。插件会生成 `RatopiaMod.YunQing.All.dll`、`translate-kv.json` 和 `RatopiaMod.YunQing.YunQingAll.cfg`。常用快捷键：

- `F9`：打开控制面板。
- `F3`：打开调试控制台。
- `F4`：打开地形控制台。

详细说明见 [RatopiaMod.Nyaiko.YunQing/README.md](RatopiaMod.Nyaiko.YunQing/README.md)。

### 研究与贸易优化（ResearchAndTradeOptimization）

解除原版研究队列和贸易协议的 3 项数量限制，保留原版研究、贸易和存档结构。研究可以预先排队，点数足够时才扣费启动；普通城市开放本地完整商品池；正在执行的普通商品贸易可以安全调整数量和期限，并按周期更新市场价。详细说明见 [ResearchAndTradeOptimization/README.md](ResearchAndTradeOptimization/README.md)。

### 卫生间澡堂加乐趣（RestroomBathFun）

鼠民完整使用普通卫生间后增加 25 点乐趣，完整使用澡堂后增加 30 点乐趣。奖励通过游戏原生 `FunUpdate` 发放，仍按原版规则封顶为 100。详细说明见 [RestroomBathFun/README.md](RestroomBathFun/README.md)。

### 共享仓库（SharedWarehouse）

让普通仓库与迷你仓库拥有无限材料种类容量，并使用同一份即时共享库存，减少仓库分散和搬运等待。详细说明见 [SharedWarehouse/README.md](SharedWarehouse/README.md)。

### 睡觉加速（SleepAcceleration）

女王在所有女王床上真正进入睡觉状态并连续 3 秒未暂停后，游戏临时切换为 5 倍速。女王离床后恢复玩家之前选择的速度；加速期间玩家主动调速会立即生效，本次睡眠不再自动加速。详细说明见 [SleepAcceleration/README.md](SleepAcceleration/README.md)。

### 特殊鼠鼠（SpecialRatizens）

“特殊鼠鼠”功能独立迁移到 BepInEx 5。本包包含 12 名特殊鼠鼠、24 个自定义特性及其图标，保留原模组的刷新概率、保底、属性、外观，以及战斗、幸福度、贸易、工作、电力和机器人效果

特殊鼠鼠特性如下表，方便玩家按鼠鼠查阅效果。“三维”指力量、手艺、智力；“初始三维”按该特殊鼠鼠生成时的三维计算。

| 特殊鼠鼠   | 特性       | 功能效果                                                                                                                         |
| ---------- | ---------- | -------------------------------------------------------------------------------------------------------------------------------- |
| 奈奈酱     | 奈奈的智慧 | 经验获取 `+100%`。                                                                                                               |
| 奈奈酱     | 奈奈的关爱 | 市民的幸福度增加 `三维 / 2`。                                                                                                    |
| 伟大嘤联邦 | 联邦的哀伤 | 吃饭时幸福度 `-50`。                                                                                                             |
| 伟大嘤联邦 | 联邦的希望 | 每次吃饭产出 `(三维 / 3) ~ (三维 / 2)` 金矿石；食物消耗速度 `+100%`。                                                            |
| 商鞅       | 垦草令     | 城市每贮存 `10` 倍人口食物时，市民幸福度 `+1`，至多 `+30`。                                                                      |
| 商鞅       | 秦律       | 可颁布的法典数量增加 `[智力 - 初始智力 + 当前城市等级 / 2]`。                                                                    |
| 岳飞       | 岳家枪     | 装备长枪时，攻击造成 `0.5` 倍范围伤害，并恢复 `伤害 * 手艺 / 100` 生命值。                                                       |
| 岳飞       | 岳家军     | 所有士兵攻击力 `+5`、防御力 `+3`、移动速度 `+30%`。                                                                              |
| 华佗       | 神医在世   | 所有市民在非受伤状态下受到致命伤害时，不会直接死亡。                                                                             |
| 华佗       | 五禽戏     | 所有市民最大生命 `+50`、工作效率 `+20%`、移动速度 `+10%`。                                                                       |
| 王亥       | 牛车       | 与他国贸易的距离缩短 `[10 + (手艺 - 初始手艺) * 4]%`。                                                                           |
| 王亥       | 商祖       | 与他国贸易的协议数量增加 `[智力 - 初始智力 + 当前城市等级 / 2]`。                                                                |
| 白圭       | 能以取予   | 物品出口价格降低 `[(1 - (智力 - 10) / (智力 - 9)) * 100]%`；与他国贸易会快速增加他国繁荣值。                                     |
| 白圭       | 商圣       | 食物贸易价格秋季降低、冬季上涨 `[智力 * 2]%`；日用品贸易价格春季降低、夏季上涨 `[智力 * 2]%`。                                   |
| 李隆基     | 开元盛世   | 所有非单人服务业可同时服务的人数增加 `[智力 - 初始智力 + 当前城市等级 / 2]`。                                                    |
| 李隆基     | 梨园       | 所有市民娱乐需求提高 `[(1 - (智力 - 10) / (智力 - 9)) * 100]%`；工作效率提升 `[智力 * 1.5]%`。                                   |
| 大正       | 蘑菇教主   | 蘑菇农场生产速度增加 `[10 + (智力 - 初始智力) * 5]%`。                                                                           |
| 大正       | 蘑菇之力   | 所有市民食用含有蘑菇的食物后，体力消耗减少 `+30%`、工作效率 `+10%`、移动速度 `+10%`。                                            |
| 赵云       | 七探蛇盘枪 | 装备长枪时，攻击造成 `0.5` 倍范围伤害并击退敌方。                                                                                |
| 赵云       | 龙胆       | 自身移动速度 `+30%`、闪避 `+50%`。                                                                                               |
| 皮卡丘     | 十万伏特   | 在发电站工作时，有 `[(力量 + 智力) * 2]%` 概率额外增加 `[力量 * (智力 / 2 ~ 智力 * 2)]` 电力，并有 `[15 - 智力]%` 概率损坏建筑。 |
| 皮卡丘     | 电气场地   | 工作时溢出的电力恢复皮卡丘的体力，并使所有市民移动速度 `+[敏捷]%`。                                                              |
| 奥米伽-7   | 量子电网   | 将自身与所有电网及电力建筑连接，并使所有电力使用效率 `+[三维 / 3]%`。                                                            |
| 奥米伽-7   | 量子机械   | 奥米伽-7无法死亡；与所有机械单位连接共享电力，并使其获得 `[50 + 三维 / 3]%` 自身三维。                                           |

### 更强大的工作距离（StrongerWorkDistance）

扩大所有使用通用鼠民工具站位的工作范围：横向 2 格、最高 4 格，包含斜角在内的完整 25 格矩形。覆盖常规采矿、建造、拆除、维修和特殊蓝图站位工作，不改变女王操作距离、战斗射程或建筑效果范围。详细说明见 [StrongerWorkDistance/README.md](StrongerWorkDistance/README.md)。

### 超级弓箭（SuperBow）

加强女王使用的原版 `WoodBow`。基础 ATK 从 2 提升到 3；重铸候选池新增“范围攻击”和“流血”词条，范围攻击可对主目标周围的其他可伤目标造成额外伤害，流血按目标最大生命值周期结算。详细说明见 [SuperBow/README.md](SuperBow/README.md)。

### 电线可穿墙（WireThroughWalls）

把普通电线从前景占位中分离，让电线能够与墙、道路和其他建筑共用同一格，同时保留供电连接、施工、取消、拆除、高亮选择和读档行为。支持已建成对象与蓝图的两种放置顺序。详细说明见 [WireThroughWalls/README.md](WireThroughWalls/README.md)。

## 构建、发布与依赖

- GitHub Actions 会构建仓库内全部非测试 Mod。
- 单个 Mod 发布使用类似 `yunqing-v3.0.0`、`superbow-v0.1.2` 的 Git Tag。
- 每个 Mod Release 同时附带当前 Mod 包和全 Mod 集合包 `Ratopia-Mod-<时间戳>.zip`。
- CI 依赖保存在私有仓库中，不在本仓库提交游戏 DLL。
- 单个 Mod 的更新说明位于 `docs/release-notes/<tag>.md`；没有对应文件时使用 `docs/release-notes/DEFAULT.md`。
