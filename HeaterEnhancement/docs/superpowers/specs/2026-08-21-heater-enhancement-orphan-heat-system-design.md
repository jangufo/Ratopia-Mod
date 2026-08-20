# 加热器覆盖外 HeatSystem 清理设计

## 问题

实机确认冬季存在远离所有加热器范围的丛林树继续增加生长度。当前游戏程序集规定：冬季植物只有在拥有 `HeatSystem` 时才跳过 `StopGrowing()`；`GardenSpeed` 只改变生长增量，不能让丛林树解除冬季暂停。因此该现象表明实时植物保留了不属于当前加热器覆盖的孤立 `HeatSystem`。

当前 Mod 会在保存、读档、范围替换、加热器消失和拆除时清理 Buff，但运行时同步只维护加热器注册表，不审计全部活植物。异常产生且未包含在旧注册范围中的 `HeatSystem` 可以继续存在。

真实程序集进一步确认了两个确定的泄漏入口：冬季 `Building_Update3()` 在 `WireCheck(true)` 失败时只播放停机动画，不调用 `BuildingWorkingStop()`；某格在供热后才被墙体阻挡时，`BuildingWorkingStop()` 会把它加入新的 `List_BlockPos`，并因此跳过该格的 `ApplyBuff(false)`。这两条路径都会让先前写入的 `HeatSystem` 留在已经不再实际供热的植物上。

## 方案比较

1. 维护独立的“实际有效覆盖”注册表，并在相关生命周期事件后标记 dirty；只在 dirty 时审计带 `HeatSystem` 的活植物。该方案同时反映季节、激活、供电和墙体阻挡，避免稳定会话中的周期性全图扫描，作为本次采用方案。
2. 只按扩展矩形的几何注册表判断。实现简单，但会错误保留墙后、断电或停用加热器范围内的孤立 Buff，不能代表实际作用效果。
3. 补丁 `WorldObject.AddBuff` 或 `ResumeGrowing`。可在调用点拦截，但会影响所有植物 Buff 来源，并与原版 `Building_Heater.ApplyBuff` 的后续恢复逻辑耦合，风险较高。

## 运行时保证与边界

- 在会话首次同步以及任一已知加热器覆盖失效事件完成后，冬季带 `HeatSystem` 的植物必须至少有一个实际占格位于“实际有效覆盖”注册表中，否则 `RemoveBuff("HeatSystem")`。
- 在会话首次同步、切换到非冬季或加热器状态批次完成后，任何活植物都不应保留 `HeatSystem`。
- 多格植物只要任一占格被覆盖就保留 Buff，避免边界上的大型植物被误清理。
- 一个格子只有同时满足以下条件才属于实际有效覆盖：当前为冬季、加热器已激活、`m_ElecNum == 1`、位于 `List_LocalPos`，并且不在原版 `List_BlockPos` 中。
- 多台加热器重叠时，只要仍有一台加热器对该格有效就保留覆盖。
- Unity 已销毁对象、空对象、空 Buff 列表和空占格必须安全跳过。
- 现有 0.5 秒入口只负责同步加热器并消费 dirty 标记；稳定状态不重复全图审计，不新增存档字段。
- 本 Mod 不全局补丁 `WorldObject.AddBuff`；若第三方 Mod 在稳定状态下直接写入孤立 `HeatSystem` 且不触发任何已知加热器生命周期，本 Mod 不承诺立即发现，下一次会话同步或覆盖失效事件会清理。

## 结构

- 保留现有几何 `Coverage` 注册表用于范围替换和拆除时的局部清理；新增独立 `EffectiveCoverage` 注册表，不能混用两者语义。
- `HeaterEffectiveCoverageCalculator` 以候选范围、阻挡格、季节、激活和供电状态生成稳定顺序的有效格，作为可脱离 Unity 测试的纯逻辑层。
- `HeaterCoverageRegistry.CoversAny(IEnumerable<GridPoint>)` 提供多格植物的覆盖判断。
- `HeaterRuntime` 在原版 `Building_Update` 完成后读取刚计算出的 `List_BlockPos`，同时再次验证 `m_Activation` 和 `m_ElecNum == 1` 后更新有效覆盖；异常退出时撤销该加热器的有效覆盖。`WireCheck` 失败、`BuildingWorkingStop`、停用、切季、拆除、加热器消失和会话重置时也撤销对应有效覆盖并标记 dirty。这些事件只更新注册表和 dirty，不在单台加热器的中间状态立刻全量审计。
- `RemoveOrphanedHeatSystemBuffs(...)` 只在 dirty 时遍历 `GameMgr.Instance._EnvMgr.List_WorldObj`，只对带 `HeatSystem` 的植物调用 `GetSizeRect()`。`TickSafely` 在枚举并同步全部加热器、移除失踪加热器后消费一次；`ReapplyAllSeasonStates` 在 `WeatherMgr.SeasonState_Update` 或 `BuildingMgr.RefreshElecUseBuilding` 的 Postfix 中同步全部已跟踪加热器后消费一次。管理器不可用时保留 dirty，等待下一次有效批次。
- 仅在实际移除至少一个 Buff 时记录汇总日志，避免周期性刷屏。

## 测试与发布

- 纯逻辑测试覆盖：完全在矩形外、单格覆盖、多格植物部分覆盖、空占格、墙阻、断电、停用、非冬季和重叠注册。
- Mono.Cecil 合同测试验证：真实程序集包含 `List_BlockPos`、`m_Activation` 和 `m_ElecNum`；运行时有效覆盖读取这些状态；审计读取活植物、调用 `GetSizeRect`、使用有效覆盖的 `CoversAny` 并移除 `HeatSystem`；审计受 dirty 标记保护且由同步/季节路径消费。
- 补丁合同验证 `Building_Update` 的 Postfix、`BuildingWorkingStop`、`WireCheck` 和激活刷新都能更新或撤销有效覆盖。
- 运行时合同补充：管理器不可用时 dirty 保留、Unity 已销毁对象安全跳过、每种撤销事件都会标记 dirty、重叠加热器批处理中不进行中途全量审计。
- 保留当前范围几何、墙体判断、电力门禁及存档清理合同。
- 版本提升为 `0.1.3`；运行全部测试、Release 构建、包白名单验证后，在 Ratopia 已退出时备份 `0.1.2` 并安装。
- 自动验证不宣称游戏内行为已验收；安装后仍需实机观察同一植物在冬季范围外不再增长。
