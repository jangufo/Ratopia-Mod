using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class GameContractTests
    {
        private const string ExpectedAssemblySha256 =
            "C94847D858EED368D2082E9715D0C79DD79309631BEF861C6BEBD467306D6E1D";

        private static readonly string RatopiaDir = typeof(GameContractTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RatopiaDir")
            .Value;

        [Fact]
        public void AssemblyCSharpMatchesTheInspectedGameBuild()
        {
            using (var stream = File.OpenRead(GamePath("Ratopia_Data", "Managed", "Assembly-CSharp.dll")))
            using (var sha = SHA256.Create())
            {
                var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                Assert.Equal(ExpectedAssemblySha256, actual);
            }
        }

        [Fact]
        public void HeaterSeasonPowerAndSaveContractsMatchTheCurrentGameAssembly()
        {
            using (var module = ModuleDefinition.ReadModule(GamePath("Ratopia_Data", "Managed", "Assembly-CSharp.dll")))
            {
                var heater = FindType(module, "Building_Heater");
                Assert.Equal("Building_ElecBase", heater.BaseType.FullName);
                AssertPrivateField(heater, "List_LocalPos", "System.Collections.Generic.List`1<UnityEngine.Vector2Int>");
                AssertPrivateField(heater, "List_BlockPos", "System.Collections.Generic.List`1<UnityEngine.Vector2Int>");
                AssertMethod(heater, "BuildingSet", "System.Void");
                var directUpdate = AssertMethod(heater, "Building_Update", "System.Void");
                AssertMethod(heater, "Building_Update3", "System.Void");
                AssertMethod(heater, "BuildingWorkingStop", "System.Void", "System.Boolean");
                AssertMethod(heater, "LoadSetting3", "System.Void", "BuildingData");
                AssertCallsMethod(directUpdate, "Building_Heater", "ApplyBuff");
                var previewUpdate = AssertMethod(
                    heater,
                    "IsFunction3OK",
                    "System.Boolean",
                    "System.Int32");
                AssertReadsField(previewUpdate, "Building_Heater", "Obj_Area");
                AssertReadsField(previewUpdate, "Building_Heater", "List_LocalPos");

                var heaterPanel = FindType(module, "BuildMid_QueenSlot_2");
                var heaterPanelUpdate = AssertMethod(
                    heaterPanel,
                    "TxtUpdate",
                    "System.Void",
                    "Building");
                AssertCallsMethod(heaterPanelUpdate, "Building", "Building_Update");
                AssertLoadsInt32(heaterPanelUpdate, 380);

                var weatherManager = FindType(module, "WeatherMgr");
                AssertPublicField(weatherManager, "m_SeasonState", "SeasonState");
                AssertMethod(weatherManager, "SeasonState_Update", "System.Void", "System.Boolean", "System.Boolean");
                AssertEnumValue(module, "SeasonState", "Winter", 4);

                var buildingManager = FindType(module, "BuildingMgr");
                AssertMethod(
                    buildingManager,
                    "ConnectUseBuild",
                    "System.Void",
                    "System.Int32",
                    "System.Int32",
                    "System.Int32");
                AssertMethod(buildingManager, "RefreshElecUseBuilding", "System.Void");

                var building = FindType(module, "Building");
                AssertPublicField(building, "m_ID", "System.Int32");
                AssertPublicField(building, "m_Info", "BuildInfo");
                AssertPublicField(building, "m_ElecWire", "Build_ElecWire");
                AssertPublicField(building, "m_Activation", "System.Boolean");
                AssertPublicField(building, "m_ElecNum", "System.Int32");
                var wireCheck = AssertMethod(
                    building,
                    "WireCheck",
                    "System.Boolean",
                    "System.Boolean");
                AssertCallsMethod(wireCheck, "BuildingMgr", "ConnectUseBuild");
                AssertCallsMethod(wireCheck, "Building", "UseWatt");
                AssertCallsMethod(wireCheck, "Building", "AlarmSet");

                var electricBuilding = FindType(module, "Building_ElecBase");
                var activateCheck = AssertMethod(
                    electricBuilding,
                    "ActivateCheck",
                    "System.Void",
                    "System.Boolean",
                    "System.Boolean");
                AssertCallsMethod(activateCheck, "Building", "ActivateCheck");
                AssertCallsMethod(activateCheck, "Building", "AlarmSet");
                AssertLoadsInt32(activateCheck, 6);
                AssertLoadsInt32(activateCheck, 8);

                var miningBox = FindType(module, "MiningBox");
                var constructionPreview = AssertMethod(
                    miningBox,
                    "BuildEnableCheck",
                    "System.Int32");
                AssertReadsField(constructionPreview, "MiningBox", "m_BuildInfo");
                AssertReadsField(constructionPreview, "MiningBox", "Tf");
                AssertCallsMethod(constructionPreview, "BuildingMgr", "GetTileList");
                AssertPrivateField(
                    miningBox,
                    "m_WaterTileCondition",
                    "MiningBox_WaterTileCondition");
                AssertMethod(miningBox, "EscapeFunction", "System.Void", "System.Boolean");
                AssertEnumValue(module, "BuildingName", "Heater", 380);

                var waterTileCondition = FindType(module, "MiningBox_WaterTileCondition");
                var waterTilePreview = AssertMethod(
                    waterTileCondition,
                    "WaterTileConditionSet",
                    "System.Void",
                    "BuildInfo",
                    "System.Int32");
                AssertLoadsInt32(waterTilePreview, 380);

                var plantData = FindType(module, "PlantData");
                var constructor = AssertMethod(plantData, ".ctor", "System.Void", "WorldObject");
                AssertPublicField(plantData, "List_BuffName", "System.Collections.Generic.List`1<System.String>");
                AssertPublicField(plantData, "List_BuffValue", "System.Collections.Generic.List`1<System.Single>");
                AssertReadsField(constructor, "WorldObject", "List_BuffName");
                AssertReadsField(constructor, "WorldObject", "List_BuffValue");

                var worldObject = FindType(module, "WorldObject");
                var plantLoad = AssertMethod(
                    worldObject,
                    "LoadSetting",
                    "System.Void",
                    "PlantData");
                AssertReadsField(plantLoad, "PlantData", "List_BuffName");
                AssertCallsMethod(plantLoad, "WorldObject", "AddBuff");
                AssertMethod(
                    worldObject,
                    "GetSizeRect",
                    "System.Collections.Generic.List`1<UnityEngine.Vector2>");
                AssertMethod(worldObject, "RemoveBuff", "System.Void", "System.String");

                var environmentManager = FindType(module, "EnvironmentMgr");
                AssertPublicField(
                    environmentManager,
                    "List_WorldObj",
                    "System.Collections.Generic.List`1<WorldObject>");

                var gameManager = FindType(module, "GameMgr");
                AssertPublicField(gameManager, "_EnvMgr", "EnvironmentMgr");

                AssertDefinesHeatSystemValue(module);
            }
        }

        private static string GamePath(params string[] parts)
        {
            var path = RatopiaDir;
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        private static TypeDefinition FindType(ModuleDefinition module, string name)
        {
            var type = module.Types.SingleOrDefault(candidate => candidate.FullName == name);
            Assert.NotNull(type);
            return type;
        }

        private static MethodDefinition AssertMethod(
            TypeDefinition type,
            string name,
            string returnType,
            params string[] parameterTypes)
        {
            var method = type.Methods.SingleOrDefault(candidate =>
                candidate.Name == name &&
                candidate.Parameters.Select(parameter => parameter.ParameterType.FullName)
                    .SequenceEqual(parameterTypes));
            Assert.NotNull(method);
            Assert.Equal(returnType, method.ReturnType.FullName);
            return method;
        }

        private static void AssertPublicField(TypeDefinition type, string name, string fieldType)
        {
            var field = type.Fields.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(field);
            Assert.True(field.IsPublic);
            Assert.Equal(fieldType, field.FieldType.FullName);
        }

        private static void AssertPrivateField(TypeDefinition type, string name, string fieldType)
        {
            var field = type.Fields.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(field);
            Assert.True(field.IsPrivate);
            Assert.Equal(fieldType, field.FieldType.FullName);
        }

        private static void AssertEnumValue(ModuleDefinition module, string typeName, string name, int value)
        {
            var field = FindType(module, typeName).Fields.Single(candidate => candidate.Name == name);
            Assert.Equal(value, Convert.ToInt32(field.Constant));
        }

        private static void AssertReadsField(MethodDefinition method, string typeName, string fieldName)
        {
            Assert.Contains(method.Body.Instructions, instruction =>
                instruction.Operand is FieldReference field &&
                field.DeclaringType.FullName == typeName &&
                field.Name == fieldName);
        }

        private static void AssertCallsMethod(
            MethodDefinition method,
            string declaringType,
            string methodName)
        {
            Assert.Contains(method.Body.Instructions, instruction =>
                instruction.Operand is MethodReference calledMethod &&
                calledMethod.DeclaringType.FullName == declaringType &&
                calledMethod.Name == methodName);
        }

        private static void AssertLoadsInt32(MethodDefinition method, int value)
        {
            Assert.Contains(method.Body.Instructions, instruction =>
                instruction.Operand is int operand && operand == value ||
                value == -1 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_M1 ||
                value == 0 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_0 ||
                value == 1 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_1 ||
                value == 2 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_2 ||
                value == 3 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_3 ||
                value == 4 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_4 ||
                value == 5 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_5 ||
                value == 6 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_6 ||
                value == 7 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_7 ||
                value == 8 && instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_8);
        }

        private static void AssertDefinesHeatSystemValue(ModuleDefinition module)
        {
            var initializer = FindType(module, "Defines").Methods.Single(method => method.Name == ".cctor");
            var instructions = initializer.Body.Instructions;
            Assert.Contains(Enumerable.Range(0, instructions.Count - 1), index =>
                instructions[index].OpCode.Code == Mono.Cecil.Cil.Code.Ldstr &&
                Equals(instructions[index].Operand, "HeatSystem") &&
                instructions[index + 1].Operand is FieldReference field &&
                field.DeclaringType.FullName == "Defines" &&
                field.Name == "Str_PBuff_HeatSystem");
        }
    }
}
