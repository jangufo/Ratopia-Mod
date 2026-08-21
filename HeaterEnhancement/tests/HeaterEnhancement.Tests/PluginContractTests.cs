using System.Linq;
using Mono.Cecil;
using HeaterEnhancement.Core;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class PluginContractTests
    {
        [Fact]
        public void PluginMetadataAndLifecyclePatchTypesMatchTheReleaseContract()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var plugin = assembly.MainModule.GetType("HeaterEnhancement.Plugin");
                Assert.NotNull(plugin);

                var attribute = plugin.CustomAttributes.Single(
                    item => item.AttributeType.FullName == "BepInEx.BepInPlugin");
                Assert.Equal("cn.ratopia.heaterenhancement", attribute.ConstructorArguments[0].Value);
                Assert.Equal("加热器加强优化", attribute.ConstructorArguments[1].Value);
                Assert.Equal("0.1.3", attribute.ConstructorArguments[2].Value);

                foreach (var patchType in new[]
                         {
                             "HeaterEnhancement.Patches.HeaterBuildingSetPatch",
                             "HeaterEnhancement.Patches.HeaterBuildingUpdatePatch",
                             "HeaterEnhancement.Patches.HeaterWorkingStopPatch",
                             "HeaterEnhancement.Patches.HeaterLoadPatch",
                             "HeaterEnhancement.Patches.HeaterRuntimeTickPatch",
                             "HeaterEnhancement.Patches.WeatherSeasonStatePatch",
                             "HeaterEnhancement.Patches.ElectricityRefreshPatch",
                             "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch",
                             "HeaterEnhancement.Patches.HeaterWireCheckPatch",
                             "HeaterEnhancement.Patches.HeaterPreviewPatch",
                             "HeaterEnhancement.Patches.HeaterConstructionPreviewPatch",
                             "HeaterEnhancement.Patches.HeaterConstructionPreviewCleanupPatch",
                             "HeaterEnhancement.Patches.HeaterActivateCheckPatch",
                             "HeaterEnhancement.Patches.PlantDataSavePatch",
                             "HeaterEnhancement.Patches.PlantLoadPatch",
                             "HeaterEnhancement.Patches.BuildingDemolitionPatch",
                             "HeaterEnhancement.Patches.LoadingSceneStartPatch"
                         })
                {
                    Assert.NotNull(assembly.MainModule.GetType(patchType));
                }

                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var runtimeTick = runtime.Methods.Single(method => method.Name == "TickSafely");
                Assert.Contains(runtimeTick.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "UnityEngine.Time" &&
                    method.Name == "get_unscaledTime");

                var ensureTracked = runtime.Methods.Single(method => method.Name == "EnsureHeaterTracked");
                Assert.DoesNotContain(ensureTracked.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "ApplySeasonState");

                var allowOriginalUpdate = runtime.Methods.Single(
                    method => method.Name == "AllowOriginalHeaterUpdate");
                Assert.DoesNotContain(allowOriginalUpdate.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.Name == "ContainsKey");
            }
        }

        [Fact]
        public void RuntimeAuditRemovesOrphanedHeatSystemBuffsFromLivePlants()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var audit = runtime.Methods.SingleOrDefault(
                    method => method.Name == "RemoveOrphanedHeatSystemBuffs");

                Assert.NotNull(audit);
                Assert.True(audit.IsPrivate);
                Assert.Equal("System.Void", audit.ReturnType.FullName);
                Assert.Equal(
                    new[] { "SeasonState", "EnvironmentMgr" },
                    audit.Parameters.Select(parameter => parameter.ParameterType.FullName));
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName ==
                    "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    field.Name == "_orphanAuditDirty");
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "EnvironmentMgr" &&
                    field.Name == "List_WorldObj");
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "WorldObject" &&
                    field.Name == "List_BuffName");
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr &&
                    Equals(instruction.Operand, "HeatSystem"));
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "WorldObject" &&
                    method.Name == "GetSizeRect");
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName ==
                    "HeaterEnhancement.Core.HeaterCoverageRegistry" &&
                    method.Name == "CoversAny");
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "WorldObject" &&
                    method.Name == "RemoveBuff");
                Assert.True(audit.Body.Instructions.Count(instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "UnityEngine.Object" &&
                    method.Name == "op_Equality") >= 2);
                Assert.Contains(audit.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName ==
                    "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    field.Name == "EffectiveCoverage");
                Assert.DoesNotContain(audit.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName ==
                    "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    field.Name == "Coverage");

                var worldObjectsRead = audit.Body.Instructions.Select((instruction, index) =>
                        new { instruction, index })
                    .First(item =>
                        item.instruction.Operand is FieldReference field &&
                        field.DeclaringType.FullName == "EnvironmentMgr" &&
                        field.Name == "List_WorldObj")
                    .index;
                var dirtyClear = audit.Body.Instructions.Select((instruction, index) =>
                        new { instruction, index })
                    .First(item =>
                        item.instruction.Operand is FieldReference field &&
                        field.DeclaringType.FullName ==
                        "HeaterEnhancement.Runtime.HeaterRuntime" &&
                        field.Name == "_orphanAuditDirty" &&
                        item.instruction.OpCode.Code == Mono.Cecil.Cil.Code.Stsfld)
                    .index;
                Assert.True(dirtyClear > worldObjectsRead);

                foreach (var callerName in new[] { "TickSafely", "ReapplyAllSeasonStates" })
                {
                    var caller = runtime.Methods.Single(method => method.Name == callerName);
                    Assert.Contains(caller.Body.Instructions, instruction =>
                        instruction.Operand is MethodReference method &&
                        method.DeclaringType.FullName ==
                        "HeaterEnhancement.Runtime.HeaterRuntime" &&
                        method.Name == "RemoveOrphanedHeatSystemBuffs");
                    Assert.Contains(caller.Body.Instructions, instruction =>
                        instruction.Operand is FieldReference field &&
                        field.DeclaringType.FullName == "GameMgr" &&
                        field.Name == "_EnvMgr");
                }

                var tick = runtime.Methods.Single(method => method.Name == "TickSafely");
                AssertCallOccursAfter(tick, "RemoveMissingHeaters", "RemoveOrphanedHeatSystemBuffs");
                var reapply = runtime.Methods.Single(method => method.Name == "ReapplyAllSeasonStates");
                AssertCallOccursAfter(reapply, "ApplySeasonState", "RemoveOrphanedHeatSystemBuffs");

                var auditCallers = runtime.Methods.Where(method => method.HasBody &&
                    method.Body.Instructions.Any(instruction =>
                        instruction.Operand is MethodReference called &&
                        called.DeclaringType.FullName ==
                        "HeaterEnhancement.Runtime.HeaterRuntime" &&
                        called.Name == "RemoveOrphanedHeatSystemBuffs"));
                Assert.Equal(
                    new[] { "ReapplyAllSeasonStates", "TickSafely" },
                    auditCallers.Select(method => method.Name).OrderBy(name => name));
            }
        }

        [Fact]
        public void EffectiveCoverageLifecycleUsesSeparateStateAndDefersAuditsToBatchBoundaries()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                Assert.NotNull(runtime.Fields.SingleOrDefault(field =>
                    field.Name == "Coverage" &&
                    field.FieldType.FullName ==
                    "HeaterEnhancement.Core.HeaterCoverageRegistry"));
                Assert.NotNull(runtime.Fields.SingleOrDefault(field =>
                    field.Name == "EffectiveCoverage" &&
                    field.FieldType.FullName ==
                    "HeaterEnhancement.Core.HeaterCoverageRegistry"));
                Assert.NotNull(runtime.Fields.SingleOrDefault(field =>
                    field.Name == "BlockedPositionField" &&
                    field.FieldType.FullName == "System.Reflection.FieldInfo"));
                Assert.NotNull(runtime.Fields.SingleOrDefault(field =>
                    field.Name == "_orphanAuditDirty" &&
                    field.FieldType.FullName == "System.Boolean"));

                var synchronize = runtime.Methods.SingleOrDefault(
                    method => method.Name == "SynchronizeEffectiveCoverage");
                var invalidate = runtime.Methods.SingleOrDefault(
                    method => method.Name == "InvalidateEffectiveCoverage");
                Assert.NotNull(synchronize);
                Assert.NotNull(invalidate);
                AssertReadsField(synchronize, "Building", "m_Activation");
                AssertReadsField(synchronize, "Building", "m_ElecNum");
                AssertCallsMethod(
                    synchronize,
                    "HeaterEnhancement.Core.HeaterEffectiveCoverageCalculator",
                    "Create");
                AssertCallsMethod(
                    synchronize,
                    "HeaterEnhancement.Core.HeaterCoverageRegistry",
                    "Register");
                AssertCallsMethod(
                    invalidate,
                    "HeaterEnhancement.Core.HeaterCoverageRegistry",
                    "Unregister");
                Assert.Contains(invalidate.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference called &&
                    called.Name == "get_Count");

                var completed = runtime.Methods.SingleOrDefault(
                    method => method.Name == "OnHeaterWorkingUpdateCompleted");
                Assert.NotNull(completed);
                AssertCallsMethod(
                    completed,
                    "HeaterEnhancement.Runtime.HeaterRuntime",
                    "SynchronizeEffectiveCoverage");
                AssertCallsMethod(
                    completed,
                    "HeaterEnhancement.Runtime.HeaterRuntime",
                    "InvalidateEffectiveCoverage");

                foreach (var methodName in new[]
                         {
                             "OnHeaterWireCheckCompleted",
                             "OnHeaterWorkingStopped",
                             "DisableForNonWinter",
                             "OnBuildingDemolishing",
                             "RemoveMissingHeaters"
                         })
                {
                    var method = runtime.Methods.SingleOrDefault(candidate => candidate.Name == methodName);
                    Assert.NotNull(method);
                    AssertCallsMethod(
                        method,
                        "HeaterEnhancement.Runtime.HeaterRuntime",
                        "InvalidateEffectiveCoverage");
                    Assert.DoesNotContain(method.Body.Instructions, instruction =>
                        instruction.Operand is MethodReference called &&
                        called.Name == "RemoveOrphanedHeatSystemBuffs");
                }

                var reset = runtime.Methods.Single(method => method.Name == "ResetSession");
                AssertCallsMethod(
                    reset,
                    "HeaterEnhancement.Core.HeaterCoverageRegistry",
                    "Clear");
                Assert.Contains(reset.Body.Instructions, instruction =>
                    instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName ==
                    "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    field.Name == "_orphanAuditDirty" &&
                    instruction.OpCode.Code == Mono.Cecil.Cil.Code.Stsfld);

                var workingUpdatePatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch");
                var workingStopPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWorkingStopPatch");
                var wireCheckPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWireCheckPatch");
                Assert.NotNull(workingStopPatch);
                AssertPatchTarget(workingStopPatch, "Building_Heater", "BuildingWorkingStop");
                AssertCallsRuntime(
                    workingUpdatePatch,
                    "Finalizer",
                    "OnHeaterWorkingUpdateCompleted");
                var workingUpdateFinalizer = workingUpdatePatch.Methods.Single(
                    method => method.Name == "Finalizer");
                Assert.Contains(workingUpdateFinalizer.Parameters, parameter =>
                    parameter.Name == "__runOriginal" &&
                    parameter.ParameterType.FullName == "System.Boolean");
                AssertCallsRuntime(workingStopPatch, "Prefix", "OnHeaterWorkingStopped");
                AssertCallsRuntime(
                    wireCheckPatch,
                    "Postfix",
                    "OnHeaterWireCheckCompleted");
            }
        }

        [Fact]
        public void DirectWorkingUpdateRequiresWinterActivationAndPowerWhileUpdate3KeepsTheSeasonGate()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var directGate = runtime.Methods.SingleOrDefault(
                    method => method.Name == "AllowOriginalHeaterWorkingUpdate");
                Assert.NotNull(directGate);
                AssertReadsField(directGate, "Building", "m_Activation");
                AssertReadsField(directGate, "Building", "m_ElecNum");
                Assert.Contains(directGate.Body.Instructions, instruction =>
                    instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_1);
                AssertCallsMethod(
                    directGate,
                    "HeaterEnhancement.Runtime.HeaterRuntime",
                    "TryGetSeasonState");

                var directPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch");
                var update3Patch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterBuildingUpdatePatch");
                AssertCallsRuntime(
                    directPatch,
                    "Prefix",
                    "AllowOriginalHeaterWorkingUpdate");
                AssertCallsRuntime(
                    directPatch,
                    "Prefix",
                    "OnHeaterWorkingUpdateSkipped");
                AssertCallsRuntime(update3Patch, "Prefix", "AllowOriginalHeaterUpdate");
                var update3Prefix = update3Patch.Methods.Single(method => method.Name == "Prefix");
                Assert.DoesNotContain(update3Prefix.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference called &&
                    called.Name == "AllowOriginalHeaterWorkingUpdate");
            }
        }

        [Fact]
        public void OriginalWorkingUpdateFailureForcesAuditEvenWithoutRegisteredCoverage()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var invalidate = runtime.Methods.Single(method =>
                    method.Name == "InvalidateEffectiveCoverage");
                Assert.Equal(
                    new[] { "System.Int32", "System.Boolean" },
                    invalidate.Parameters.Select(parameter => parameter.ParameterType.FullName));
                Assert.Equal("forceAudit", invalidate.Parameters[1].Name);
                Assert.Contains(invalidate.Body.Instructions, instruction =>
                    instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_1);

                var completed = runtime.Methods.Single(method =>
                    method.Name == "OnHeaterWorkingUpdateCompleted");
                Assert.Equal(
                    new[] { "Building_Heater", "System.Boolean", "System.Boolean" },
                    completed.Parameters.Select(parameter => parameter.ParameterType.FullName));
                Assert.Equal("originalRan", completed.Parameters[1].Name);
                Assert.Equal("succeeded", completed.Parameters[2].Name);
                AssertAllCallsUseBooleanArgument(
                    completed,
                    "InvalidateEffectiveCoverage",
                    true);

                var synchronize = runtime.Methods.Single(method =>
                    method.Name == "SynchronizeEffectiveCoverage");
                AssertContainsCallWithBooleanArgument(
                    synchronize,
                    "InvalidateEffectiveCoverage",
                    true);

                var skipped = runtime.Methods.SingleOrDefault(method =>
                    method.Name == "OnHeaterWorkingUpdateSkipped");
                Assert.NotNull(skipped);
                AssertAllCallsUseBooleanArgument(
                    skipped,
                    "InvalidateEffectiveCoverage",
                    false);

                foreach (var methodName in new[]
                         {
                             "OnHeaterWireCheckCompleted",
                             "OnHeaterWorkingStopped",
                             "DisableForNonWinter",
                             "OnBuildingDemolishing",
                             "RemoveMissingHeaters"
                         })
                {
                    AssertAllCallsUseBooleanArgument(
                        runtime.Methods.Single(method => method.Name == methodName),
                        "InvalidateEffectiveCoverage",
                        false);
                }

                var directPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch");
                var finalizer = directPatch.Methods.Single(method => method.Name == "Finalizer");
                AssertCallsRuntime(
                    directPatch,
                    "Finalizer",
                    "OnHeaterWorkingUpdateCompleted");
                Assert.Contains(finalizer.Parameters, parameter =>
                    parameter.Name == "__runOriginal" &&
                    parameter.ParameterType.FullName == "System.Boolean");
                Assert.Contains(finalizer.Parameters, parameter =>
                    parameter.Name == "__exception" &&
                    parameter.ParameterType.FullName == "System.Exception");
            }
        }

        [Fact]
        public void FullyBlockedEligibleWorkingUpdateDoesNotForceRepeatedAudit()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var synchronize = runtime.Methods.Single(method =>
                    method.Name == "SynchronizeEffectiveCoverage");

                AssertContainsCallWithBooleanArgument(
                    synchronize,
                    "InvalidateEffectiveCoverage",
                    true);
                AssertContainsCallWithBooleanArgument(
                    synchronize,
                    "InvalidateEffectiveCoverage",
                    false);
            }
        }

        [Fact]
        public void SkippedDirectUpdateDisablesOnlyConfirmedNonWinterHeaters()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                var skipped = runtime.Methods.Single(method =>
                    method.Name == "OnHeaterWorkingUpdateSkipped");

                AssertCallsMethod(
                    skipped,
                    "HeaterEnhancement.Runtime.HeaterRuntime",
                    "TryGetSeasonState");
                AssertCallsMethod(
                    skipped,
                    "HeaterEnhancement.Runtime.HeaterRuntime",
                    "DisableForNonWinter");
                AssertContainsCallWithBooleanArgument(
                    skipped,
                    "InvalidateEffectiveCoverage",
                    false);
            }
        }

        [Fact]
        public void NonWinterBypassPatchesTargetBothDirectUpdateAndWireCheck()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var directUpdatePatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch");
                var wireCheckPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterWireCheckPatch");

                Assert.NotNull(directUpdatePatch);
                Assert.NotNull(wireCheckPatch);
                AssertPatchTarget(directUpdatePatch, "Building_Heater", "Building_Update");
                AssertPatchTarget(wireCheckPatch, "Building", "WireCheck");

                var harmonyAfter = wireCheckPatch.CustomAttributes.SingleOrDefault(
                    attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyAfter");
                Assert.NotNull(harmonyAfter);
                var harmonyIds = (CustomAttributeArgument[])harmonyAfter.ConstructorArguments[0].Value;
                Assert.Contains(harmonyIds, argument =>
                    (string)argument.Value == "cn.ratopia.specialratizens");

                var postfix = wireCheckPatch.Methods.Single(method => method.Name == "Postfix");
                Assert.Contains(postfix.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "EnforceNonWinterWireResult");

                var prefix = wireCheckPatch.Methods.Single(method => method.Name == "Prefix");
                Assert.Equal("System.Boolean", prefix.ReturnType.FullName);
                Assert.Contains(prefix.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "SuppressNonWinterWireCheck");
            }
        }

        [Fact]
        public void PreviewLoadAndStaleCoveragePatchesUseTheRuntimeCoordinator()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var previewPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterPreviewPatch");
                var plantLoadPatch = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.PlantLoadPatch");

                Assert.NotNull(previewPatch);
                Assert.NotNull(plantLoadPatch);
                AssertPatchTarget(previewPatch, "Building_Heater", "IsFunction3OK");
                AssertPatchTarget(plantLoadPatch, "WorldObject", "LoadSetting");
                AssertCallsRuntime(previewPatch, "Prefix", "PreparePreviewRange");
                AssertCallsRuntime(previewPatch, "Postfix", "HideLegacyPreviewArea");
                AssertCallsRuntime(plantLoadPatch, "Prefix", "SanitizePlantLoadData");

                var runtime = assembly.MainModule.GetType(
                    "HeaterEnhancement.Runtime.HeaterRuntime");
                var sanitizeSave = runtime.Methods.Single(
                    method => method.Name == "SanitizePlantSaveData");
                Assert.DoesNotContain(sanitizeSave.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName ==
                    "HeaterEnhancement.Core.HeaterCoverageRegistry" &&
                    method.Name == "Covers");

                foreach (var methodName in new[] { "RegisterExtendedRange", "RemoveMissingHeaters" })
                {
                    var method = runtime.Methods.Single(candidate => candidate.Name == methodName);
                    Assert.Contains(method.Body.Instructions, instruction =>
                        instruction.Operand is MethodReference called &&
                        called.DeclaringType.FullName ==
                        "HeaterEnhancement.Runtime.HeaterRuntime" &&
                        called.Name == "RemoveHeatSystemAt");
                }
            }
        }

        [Fact]
        public void ConstructionPreviewAlarmAndLiveRangeSynchronizationUseDedicatedRuntimePaths()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var constructionPreview = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterConstructionPreviewPatch");
                var constructionCleanup = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterConstructionPreviewCleanupPatch");
                var activateCheck = assembly.MainModule.GetType(
                    "HeaterEnhancement.Patches.HeaterActivateCheckPatch");

                Assert.NotNull(constructionPreview);
                Assert.NotNull(constructionCleanup);
                Assert.NotNull(activateCheck);
                AssertPatchTarget(constructionPreview, "MiningBox", "BuildEnableCheck");
                AssertPatchTarget(constructionCleanup, "MiningBox", "EscapeFunction");
                AssertPatchTarget(activateCheck, "Building_ElecBase", "ActivateCheck");
                AssertCallsRuntime(constructionPreview, "Postfix", "UpdateConstructionPreview");
                AssertCallsRuntime(constructionCleanup, "Prefix", "HideConstructionPreview");
                AssertCallsRuntime(activateCheck, "Postfix", "SuppressNonWinterPowerAlarm");

                var runtime = assembly.MainModule.GetType(
                    "HeaterEnhancement.Runtime.HeaterRuntime");
                var preparePreview = runtime.Methods.Single(method => method.Name == "PreparePreviewRange");
                Assert.DoesNotContain(preparePreview.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "SetLocalPositions");
                Assert.Contains(preparePreview.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "SynchronizeExtendedRange");

                var updateConstructionPreview = runtime.Methods.Single(
                    method => method.Name == "UpdateConstructionPreview");
                Assert.Contains(updateConstructionPreview.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "CreateCoveragePoints");
                Assert.Contains(updateConstructionPreview.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "GreenBox" &&
                    method.Name == "G_BoxSet");
                Assert.Contains(updateConstructionPreview.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "HideLegacyConstructionPreview");

                var suppressAlarm = runtime.Methods.Single(
                    method => method.Name == "SuppressNonWinterPowerAlarm");
                Assert.Contains(suppressAlarm.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "Building" &&
                    method.Name == "AlarmSet");

                var resetSession = runtime.Methods.Single(method => method.Name == "ResetSession");
                Assert.Contains(resetSession.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "HideConstructionPreview");
            }
        }

        [Fact]
        public void ShutdownRestoresEachTrackedHeaterWithIndependentBestEffortGuards()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType(
                    "HeaterEnhancement.Runtime.HeaterRuntime");
                var shutdown = runtime.Methods.Single(method => method.Name == "Shutdown");
                var restore = runtime.Methods.SingleOrDefault(
                    method => method.Name == "RestoreHeaterOnShutdown");

                Assert.NotNull(restore);
                Assert.Contains(shutdown.Body.ExceptionHandlers, handler =>
                    handler.HandlerType == Mono.Cecil.Cil.ExceptionHandlerType.Catch);
                Assert.Contains(shutdown.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "RestoreHeaterOnShutdown");
                Assert.True(restore.Body.ExceptionHandlers.Count >= 2);
                Assert.Contains(restore.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "ClearCurrentRange");
                Assert.Contains(restore.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method &&
                    method.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                    method.Name == "SetLocalPositions");
            }
        }

        private static void AssertPatchTarget(
            TypeDefinition patchType,
            string targetType,
            string targetMethod)
        {
            var harmonyPatch = patchType.CustomAttributes.Single(
                attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch");
            Assert.Equal(targetType, ((TypeReference)harmonyPatch.ConstructorArguments[0].Value).FullName);
            Assert.Equal(targetMethod, harmonyPatch.ConstructorArguments[1].Value);
        }

        private static void AssertCallsRuntime(
            TypeDefinition patchType,
            string patchMethodName,
            string runtimeMethodName)
        {
            var patchMethod = patchType.Methods.Single(method => method.Name == patchMethodName);
            Assert.Contains(patchMethod.Body.Instructions, instruction =>
                instruction.Operand is MethodReference called &&
                called.DeclaringType.FullName == "HeaterEnhancement.Runtime.HeaterRuntime" &&
                called.Name == runtimeMethodName);
        }

        private static void AssertReadsField(
            MethodDefinition method,
            string declaringType,
            string fieldName)
        {
            Assert.Contains(method.Body.Instructions, instruction =>
                instruction.Operand is FieldReference field &&
                field.DeclaringType.FullName == declaringType &&
                field.Name == fieldName);
        }

        private static void AssertCallsMethod(
            MethodDefinition method,
            string declaringType,
            string methodName)
        {
            Assert.Contains(method.Body.Instructions, instruction =>
                instruction.Operand is MethodReference called &&
                called.DeclaringType.FullName == declaringType &&
                called.Name == methodName);
        }

        private static void AssertCallOccursAfter(
            MethodDefinition method,
            string earlierMethod,
            string laterMethod)
        {
            var calls = method.Body.Instructions.Select((instruction, index) =>
                    new { instruction, index })
                .Where(item => item.instruction.Operand is MethodReference)
                .Select(item => new
                {
                    method = (MethodReference)item.instruction.Operand,
                    item.index
                })
                .ToArray();
            var earlier = calls.Last(item => item.method.Name == earlierMethod).index;
            var later = calls.Single(item => item.method.Name == laterMethod).index;
            Assert.True(later > earlier);
        }

        private static void AssertAllCallsUseBooleanArgument(
            MethodDefinition method,
            string calledMethodName,
            bool expected)
        {
            var calls = method.Body.Instructions.Select((instruction, index) =>
                    new { instruction, index })
                .Where(item =>
                    item.instruction.Operand is MethodReference called &&
                    called.Name == calledMethodName)
                .ToArray();
            Assert.NotEmpty(calls);
            foreach (var call in calls)
            {
                var argument = method.Body.Instructions[call.index - 1].OpCode.Code;
                Assert.Equal(
                    expected
                        ? Mono.Cecil.Cil.Code.Ldc_I4_1
                        : Mono.Cecil.Cil.Code.Ldc_I4_0,
                    argument);
            }
        }

        private static void AssertContainsCallWithBooleanArgument(
            MethodDefinition method,
            string calledMethodName,
            bool expected)
        {
            Assert.Contains(method.Body.Instructions.Select((instruction, index) =>
                    new { instruction, index }), item =>
                item.index > 0 &&
                item.instruction.Operand is MethodReference called &&
                called.Name == calledMethodName &&
                method.Body.Instructions[item.index - 1].OpCode.Code ==
                (expected
                    ? Mono.Cecil.Cil.Code.Ldc_I4_1
                    : Mono.Cecil.Cil.Code.Ldc_I4_0));
        }
    }
}
