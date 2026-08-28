using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using Xunit;

namespace MultipurposeResearch.Tests
{
    public sealed class GameContractTests
    {
        private const string ExpectedAssemblySha256 =
            "C94847D858EED368D2082E9715D0C79DD79309631BEF861C6BEBD467306D6E1D";

        [Fact]
        public void AssemblyCSharpMatchesInspectedBuild()
        {
            using (var stream = File.OpenRead(GetAssemblyPath("Assembly-CSharp.dll")))
            using (var sha = SHA256.Create())
            {
                var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                Assert.Equal(ExpectedAssemblySha256, actual);
            }
        }

        [Fact]
        public void ResearchAndLifecycleContractsRemainAvailable()
        {
            using (var module = ModuleDefinition.ReadModule(GetAssemblyPath("Assembly-CSharp.dll")))
            {
                AssertMethod(module, "ResearchUI", "PointUp", "System.Void", "System.Int32");
                AssertMethod(module, "ResearchUI", "IconTxtUpdate", "System.Void");
                AssertMethod(module, "ResearchUI", "MakeNodeGraph", "System.Void", "System.Int32");
                AssertMethod(module, "ResearchUI", "Update", "System.Void");
                AssertMethod(module, "ResearchUI", "ExitBtn", "System.Void");
                AssertMethod(module, "ResearchUI", "get_m_BuildMidUI", "BuildMidUI");
                AssertMethod(module, "ResearchListUI", "ResearchListtUI_Set", "System.Void", "System.Boolean");
                AssertMethod(module, "ResearchListUI", "InteracAction", "System.Void");
                AssertMethod(module, "ResearchCategorySlot", "PointerClick", "System.Void", "System.Boolean");
                AssertMethod(module, "TechNode", "NodeClick", "System.Void", "System.Boolean");
                AssertMethod(module, "TechNode", "LineReset", "System.Void");
                AssertMethod(module, "TechLine", "LineSet", "System.Void", "TechNode", "TechNode");
                AssertMethod(module, "TechLine", "LineUpdate", "System.Void", "System.Boolean");
                AssertMethod(module, "Tech_RPInfo", "UpgradBtn", "System.Void");
                AssertMethod(module, "Tech_RPInfo", "RightPageSet", "System.Void", "TechNode");
                AssertMethod(module, "CitizenBuff", "LogToAllRef", "System.Collections.Generic.List`1<CitizenBuff/RefInfo>", "System.Boolean");
                AssertMethod(module, "CitizenBuff/RefInfo", "Get_T_Name", "System.String", "System.String", "C_Buff_Category", "System.Boolean");
                AssertMethod(module, "CitizenBuff/RefInfo", "GetIconAddress", "System.String", "System.String", "C_Buff_Category");
                AssertMethod(module, "CitizenBuff/RefInfo", "GetDescript", "System.String");
                AssertMethod(module, "CitizenBuff/RefInfo", "GetEffectScript", "System.String");
                AssertMethod(module, "T_UnitMgr", "LoadSettings2", "System.Void", "D_Data");
                AssertMethod(module, "T_Citizen", "CitizenInit", "System.Void", "Gender", "System.Boolean", "System.Boolean");
                AssertMethod(module, "T_Queen", "LoadSetting2", "System.Void");
                AssertMethod(module, "GameUnit", "GetPure_PDI", "System.Int32", "PDI");
                AssertMethod(module, "EconomicMgr", "Country_GetGold_Diplomatic", "System.Void", "System.String", "System.Single");
                AssertMethod(module, "CasselGames.Diplomatic.DiplomaticMgr", "GetExploredCountryArray", "CasselGames.Diplomatic.Data.DiplomaticCountryData[]");
                AssertMethod(module, "CasselGames.Diplomatic.DiplomaticMgr", "IncreaseRelations", "System.Void", "System.String", "System.Int32");
                AssertMethod(module, "CasselGames.Diplomatic.DiplomaticMgr", "IncreaseProsperity", "System.Void", "System.String", "System.Int32");
                AssertField(module, "ResearchUI", "m_Point", "System.Int32");
                AssertField(module, "ResearchUI", "Prefab_TechNode", "UnityEngine.GameObject");
                AssertField(module, "ResearchUI", "Tf_Content", "UnityEngine.RectTransform");
                AssertField(module, "ResearchUI", "Obj_CategoryGroup", "UnityEngine.GameObject");
                AssertField(module, "GameMgr", "_BuildMidUI", "BuildMidUI");
                AssertField(module, "GameMgr", "_SubBuildMidUI", "BuildMidUI");
                AssertField(module, "BuildMidUI", "Obj_Main", "UnityEngine.GameObject");
                AssertField(module, "Tech_RPInfo", "Obj_DetailTxt", "UnityEngine.GameObject");
                AssertField(module, "Tech_RPInfo", "Txt_Name", "TMPro.TextMeshProUGUI");
                AssertField(module, "Tech_RPInfo", "Txt_Description", "TMPro.TextMeshProUGUI");
                AssertField(module, "Tech_RPInfo", "Obj_UpgradeBtn", "UnityEngine.GameObject");
                AssertField(module, "Tech_RPInfo", "Txt_UpgradeBtn", "TMPro.TextMeshProUGUI");
                AssertField(module, "Tech_RPInfo", "Txt_NeedBtn", "TMPro.TextMeshProUGUI");
                AssertField(module, "Tech_RPInfo", "Txt_UpgradeTime", "TMPro.TextMeshProUGUI");
                AssertField(module, "Tech_RPInfo", "Obj_AlreadyResearch", "UnityEngine.GameObject");
                AssertField(module, "Tech_RPInfo", "m_IsEmpty", "System.Boolean");
                AssertField(module, "ResearchListUI", "m_CategorySlots", "ResearchCategorySlot[]");
                AssertField(module, "ResearchListUI", "List_Slots", "System.Collections.Generic.List`1<ResearchCategorySlot>");
                AssertField(module, "T_UnitMgr", "List_Citizen", "System.Collections.Generic.List`1<T_Citizen>");
                AssertField(module, "GameUnit", "m_Buff", "CitizenBuff");
            }
        }

        [Fact]
        public void VanillaResearchUpdateStillOwnsEscapeAndCategoryNavigation()
        {
            using (var module = ModuleDefinition.ReadModule(GetAssemblyPath("Assembly-CSharp.dll")))
            {
                var researchUi = module.Types.Single(type => type.FullName == "ResearchUI");
                var update = researchUi.Methods.Single(method => method.Name == "Update");
                var calls = update.Body.Instructions
                    .Select(instruction => instruction.Operand as MethodReference)
                    .Where(reference => reference != null && reference.DeclaringType.FullName == "ResearchUI")
                    .Select(reference => reference.Name)
                    .ToArray();

                Assert.Contains("ExitBtn", calls);
                Assert.Contains("Cat_LeftBtn", calls);
                Assert.Contains("Cat_RightBtn", calls);
            }
        }

        private static string GetAssemblyPath(string fileName)
        {
            var ratopiaDir = Environment.GetEnvironmentVariable("RATOPIA_DIR");
            if (string.IsNullOrWhiteSpace(ratopiaDir))
            {
                ratopiaDir = typeof(GameContractTests).Assembly
                    .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
                    .Cast<AssemblyMetadataAttribute>()
                    .Single(attribute => attribute.Key == "RatopiaDir")
                    .Value;
            }

            var path = Path.Combine(ratopiaDir, "Ratopia_Data", "Managed", fileName);
            Assert.True(File.Exists(path), $"Game assembly not found: {path}");
            return path;
        }

        private static void AssertMethod(ModuleDefinition module, string typeName, string name, string returnType, params string[] parameters)
        {
            var type = module.Types.SelectMany(Flatten).Single(item => item.FullName == typeName);
            Assert.Contains(type.Methods, method => method.Name == name && method.ReturnType.FullName == returnType &&
                method.Parameters.Select(parameter => parameter.ParameterType.FullName).SequenceEqual(parameters));
        }

        private static void AssertField(ModuleDefinition module, string typeName, string name, string fieldType)
        {
            var type = module.Types.SelectMany(Flatten).Single(item => item.FullName == typeName);
            var field = type.Fields.Single(item => item.Name == name);
            Assert.Equal(fieldType, field.FieldType.FullName);
        }

        private static System.Collections.Generic.IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
        {
            yield return type;
            foreach (var nested in type.NestedTypes.SelectMany(Flatten))
            {
                yield return nested;
            }
        }
    }
}
