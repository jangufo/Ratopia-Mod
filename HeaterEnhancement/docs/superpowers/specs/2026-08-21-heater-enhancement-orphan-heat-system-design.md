# 加热器覆盖外 HeatSystem 清理设计

## 问题

实机确认冬季存在远离所有加热器范围的丛林树继续增加生长度。当前游戏程序集规定：冬季植物只有在拥有 `HeatSystem` 时才跳过 `StopGrowing()`；`GardenSpeed` 只改变生长增量，不能让丛林树解除冬季暂停。因此该现象表明实时植物保留了不属于当前加热器覆盖的孤立 `HeatSystem`。

当前 Mod 会在保存、读档、范围替换、加热器消失和拆除时清理 Buff，但运行时同步只维护加热器注册表，不审计全部活植物。异常产生且未包含在旧注册范围中的 `HeatSystem` 可以继续存在。

## 方案比较

1. 在现有 0.5 秒运行时同步中审计带 `HeatSystem` 的活植物。覆盖内保留，覆盖外移除。该方案直接维护运行时不变量，不改变原版生长协程，作为本次采用方案。
2. 只在读档、切季和拆除时清理。成本较低，但不能修复会话中稍后产生的孤立 Buff，无法覆盖已确认症状。
3. 补丁 `WorldObject.AddBuff` 或 `ResumeGrowing`。可在调用点拦截，但会影响所有植物 Buff 来源，并与原版 `Building_Heater.ApplyBuff` 的后续恢复逻辑耦合，风险较高。

## 运行时不变量

- 冬季：带 `HeatSystem` 的植物必须至少有一个实际占格位于覆盖注册表中，否则立即 `RemoveBuff("HeatSystem")`。
- 非冬季：任何活植物都不应保留 `HeatSystem`。
- 多格植物只要任一占格被覆盖就保留 Buff，避免边界上的大型植物被误清理。
- 几何覆盖注册表只负责判断矩形外孤立 Buff；墙体阻挡、供电状态和 Buff 应用仍由原版加热器逻辑处理。
- 审计复用现有 0.5 秒节流入口，不新增每帧补丁或存档字段。

## 结构

- `HeaterCoverageRegistry.CoversAny(IEnumerable<GridPoint>)` 提供可脱离 Unity 测试的多格覆盖判断。
- `HeaterRuntime.RemoveOrphanedHeatSystemBuffs(...)` 遍历 `EnvironmentMgr.List_WorldObj`，只检查带 `HeatSystem` 的植物；用 `WorldObject.GetSizeRect()` 获取全部占格。
- `TickSafely` 在完成加热器注册、失效清理和季节重应用后执行审计。切季补丁的同步路径也在完成状态重应用后执行一次审计。
- 仅在实际移除至少一个 Buff 时记录汇总日志，避免周期性刷屏。

## 测试与发布

- 纯逻辑测试覆盖：完全在范围外、单格覆盖、多格植物部分覆盖、空占格。
- Mono.Cecil 合同测试验证运行时审计读取活植物、调用 `GetSizeRect`、使用 `CoversAny`、移除 `HeatSystem`，并由节流入口调用。
- 保留当前范围几何、墙体判断、电力门禁及存档清理合同。
- 版本提升为 `0.1.3`；运行全部测试、Release 构建、包白名单验证后，在 Ratopia 已退出时备份 `0.1.2` 并安装。
- 自动验证不宣称游戏内行为已验收；安装后仍需实机观察同一植物在冬季范围外不再增长。
