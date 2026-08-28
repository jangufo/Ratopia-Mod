using System;
using System.Linq;
using System.Reflection;
using HeaterEnhancement.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class PlantThermalAuditPlannerTests
    {
        [Theory]
        [InlineData(false, false, false, false, "None")]
        [InlineData(false, false, true, false, "RemoveHeatSystem")]
        [InlineData(true, true, true, false, "None")]
        [InlineData(true, false, true, false, "RemoveHeatSystemAndStopGrowing")]
        [InlineData(true, false, false, false, "StopGrowing")]
        [InlineData(true, false, false, true, "None")]
        public void PlannerEnforcesWinterPauseOutsideEffectiveCoverage(
            bool isWinter,
            bool isCovered,
            bool hasHeatSystem,
            bool isGrowingPaused,
            string expectedAction)
        {
            var assembly = typeof(GridPoint).Assembly;
            var planner = assembly.GetType("HeaterEnhancement.Core.PlantThermalAuditPlanner");

            Assert.NotNull(planner);
            var decide = planner.GetMethod(
                "Decide",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(decide);

            var actual = decide.Invoke(
                null,
                new object[] { isWinter, isCovered, hasHeatSystem, isGrowingPaused });

            Assert.Equal(expectedAction, actual.ToString());
        }

        [Fact]
        public void RuntimeAuditIsForcedAfterEverySeasonStateUpdateAndCanStopPlants()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(GridPoint).Assembly.Location))
            {
                var runtime = assembly.MainModule.GetType("HeaterEnhancement.Runtime.HeaterRuntime");
                Assert.NotNull(runtime);

                var seasonUpdated = FindMethod(runtime, "OnSeasonStateUpdated");
                Assert.Contains(
                    seasonUpdated.Body.Instructions,
                    instruction => instruction.OpCode.Code == Code.Stsfld &&
                                   ((FieldReference)instruction.Operand).Name == "_orphanAuditDirty");

                var runtimeTick = FindMethod(runtime, "TickSafely");
                Assert.Contains(
                    runtimeTick.Body.Instructions,
                    instruction => instruction.OpCode.Code == Code.Stsfld &&
                                   ((FieldReference)instruction.Operand).Name == "_orphanAuditDirty");


                var audit = FindMethod(runtime, "RemoveOrphanedHeatSystemBuffs");
                Assert.Contains(
                    audit.Body.Instructions,
                    instruction => instruction.OpCode.Code == Code.Call &&
                                   instruction.Operand is MethodReference method &&
                                   method.DeclaringType.FullName ==
                                       "HeaterEnhancement.Core.PlantThermalAuditPlanner" &&
                                   method.Name == "Decide");
                Assert.Contains(
                    audit.Body.Instructions,
                    instruction => instruction.OpCode.Code == Code.Callvirt &&
                                   instruction.Operand is MethodReference method &&
                                   method.DeclaringType.FullName == "WorldObject" &&
                                   method.Name == "StopGrowing");
            }
        }

        private static MethodDefinition FindMethod(TypeDefinition type, string name)
        {
            var method = type.Methods.FirstOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(method);
            return method;
        }
    }
}
