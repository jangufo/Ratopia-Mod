using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace MultipurposeResearch.Tests
{
    public sealed class PluginContractTests
    {
        [Fact]
        public void PluginKeepsRequestedChineseIdentity()
        {
            var path = BuiltPluginPath();
            using (var module = ModuleDefinition.ReadModule(path))
            {
                var pluginType = module.Types.Single(type => type.FullName == "MultipurposeResearch.Plugin");
                var attribute = pluginType.CustomAttributes.Single(item => item.AttributeType.FullName.Contains("BepInPlugin"));
                Assert.Equal("cn.ratopia.multipurposeresearch", (string)attribute.ConstructorArguments[0].Value);
                Assert.Equal("多用途研究点", (string)attribute.ConstructorArguments[1].Value);
                Assert.Equal("0.1.11", (string)attribute.ConstructorArguments[2].Value);
            }
        }

        [Fact]
        public void ResearchEntryUsesCurrentResearchListContract()
        {
            var sourcePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var source = File.ReadAllText(sourcePath);

            Assert.Contains("ResearchListUI", source);
            Assert.Contains("m_CategorySlots", source);
            Assert.Contains("ResearchCategorySlot", source);
            Assert.DoesNotContain("m_Tech_CatBtn", source);
        }

        [Fact]
        public void ResearchEntryIsAttachedWhenTheVanillaResearchViewOpens()
        {
            var sourcePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var source = File.ReadAllText(sourcePath);

            Assert.Contains("ResearchUiRuntime.LeaveCustom()", source);
            Assert.Contains("Research_IconBtn", source);
            Assert.Contains("EngineeringResearch_IconBtn", source);
            Assert.Contains("MagicianResearch_IconBtn", source);
        }

        [Fact]
        public void ResearchCategoryPickerUsesResearchListUiContract()
        {
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var patchSource = File.ReadAllText(patchPath);
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("ResearchListUI", patchSource);
            Assert.Contains("ResearchListUI.Awake", patchSource);
            Assert.Contains("ResearchListtUI_Set", patchSource);
            Assert.Contains("m_CategorySlots", runtimeSource);
            Assert.Contains("多用途研究点入口", runtimeSource);
        }

        [Fact]
        public void ResearchCategoryPickerSupportsMouseAndKeyboardSelection()
        {
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var patchSource = File.ReadAllText(patchPath);
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("ResearchCategorySlot", patchSource);
            Assert.Contains("InteracAction", patchSource);
            Assert.Contains("List_Slots.Add(slot)", runtimeSource);
            Assert.Contains("MultipurposeResearchListEntry", runtimeSource);
        }

        [Fact]
        public void ResearchCategoryEntryRefreshesTheVisibleLabelEveryTimeItOpens()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("RefreshResearchListEntryVisuals(_researchListEntryObject)", runtimeSource);
            Assert.Contains("foreach (var label in labels)", runtimeSource);
            Assert.Contains("label.text = \"多用途研究点\"", runtimeSource);
            Assert.DoesNotContain("labels[labels.Length - 1].text", runtimeSource);
        }

        [Fact]
        public void ResearchCategoryEntryGetsSpacingWithOnlyOneVisibleVanillaCategory()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("GetResearchListEntrySpacing(researchList, activeSlots, lastRect)", runtimeSource);
            Assert.Contains("candidateRect.anchoredPosition.x - lastRect.anchoredPosition.x", runtimeSource);
            Assert.DoesNotContain(
                "if (entryRect != null && lastRect != null && activeSlots.Length > 1)",
                runtimeSource);
        }

        [Fact]
        public void CustomNodesRemoveVanillaTechNodeClickHandler()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("DestroyImmediate(node)", runtimeSource);
            Assert.Contains("GetComponentsInChildren<TechNode>(true)", runtimeSource);
            Assert.Contains("var customNode", runtimeSource);
        }

        [Fact]
        public void CustomExecutionUsesDedicatedDetailButtonAndKeepsKeyboardExecutionPatch()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var runtimeSource = File.ReadAllText(runtimePath);
            var patchSource = File.ReadAllText(patchPath);

            Assert.Contains("CustomDetailRootName", runtimeSource);
            Assert.Contains("CreateCustomDetailView(research)", runtimeSource);
            Assert.Contains("executeButton.onClick.AddListener(ConfirmSelected)", runtimeSource);
            Assert.Contains("TryConfirmSelected", runtimeSource);
            Assert.Contains("typeof(Tech_RPInfo)", patchSource);
            Assert.Contains("nameof(Tech_RPInfo.UpgradBtn)", patchSource);
            Assert.Contains("return !ResearchUiRuntime.TryConfirmSelected()", patchSource);
        }

        [Fact]
        public void CustomViewHidesAndRestoresTheWholeVanillaContentRoot()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("_originalContentActive", runtimeSource);
            Assert.Contains("research.Tf_Content.gameObject.SetActive(false)", runtimeSource);
            Assert.Contains("research.Tf_Content.gameObject.SetActive(_originalContentActive)", runtimeSource);
            Assert.DoesNotContain("OriginalVanillaNodes", runtimeSource);
            Assert.DoesNotContain("OriginalVanillaLines", runtimeSource);
        }

        [Fact]
        public void CustomResearchUsesAContentRootOutsideTheVanillaGraph()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("CustomContentRootName", runtimeSource);
            Assert.Contains("GetOrCreateCustomContentRoot(research)", runtimeSource);
            Assert.Contains("root.SetParent(research.Tf_Content.parent, false)", runtimeSource);
            Assert.DoesNotContain("Instantiate(research.Prefab_TechNode, research.Tf_Content)", runtimeSource);
            Assert.DoesNotContain("root.SetParent(research.Tf_Content, false)", runtimeSource);
        }

        [Fact]
        public void CustomResearchShowsSelectionPromptAndButtonImmediately()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("ShowSelectionPrompt(research)", runtimeSource);
            Assert.Contains("SetCustomDetail(research", runtimeSource);
            Assert.Contains("\"多用途研究点\",", runtimeSource);
            Assert.Contains("\"请先选择左侧项目\",", runtimeSource);
            Assert.Contains("\"请选择项目\",", runtimeSource);
            Assert.Contains("ShowSelectionRequired()", runtimeSource);
            Assert.Contains("CenterAlarmCustomSet(\"请先选择左侧项目。\", Color.white)", runtimeSource);
        }

        [Fact]
        public void CustomDetailUsesAnIndependentCloneInsteadOfMutatingVanillaDetail()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("CreateCustomDetailView(research)", runtimeSource);
            Assert.Contains("originalDetail.gameObject.SetActive(false)", runtimeSource);
            Assert.Contains("UnityEngine.Object.Instantiate(originalDetail.gameObject", runtimeSource);
            Assert.Contains("view.DetailText.SetActive(true)", runtimeSource);
            Assert.Contains("view.AlreadyResearch.SetActive(false)", runtimeSource);
            Assert.Contains("view.UpgradeTime.text = string.Empty", runtimeSource);
            Assert.Contains("view.Description.text = description", runtimeSource);
            Assert.DoesNotContain("detail.Txt_Description.text = description", runtimeSource);
        }

        [Fact]
        public void CustomDetailUsesRootLevelTextThatCannotBeHiddenByVanillaSlotState()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("CreateIndependentDetailText(clonedDetail.Txt_Name, clone.transform", runtimeSource);
            Assert.Contains("CreateIndependentDetailText(clonedDetail.Txt_Description, clone.transform", runtimeSource);
            Assert.Contains("textObject.transform.SetParent(detailRoot, false)", runtimeSource);
            Assert.Contains("text.raycastTarget = false", runtimeSource);
            Assert.DoesNotContain("Name = clonedDetail.Txt_Name", runtimeSource);
            Assert.DoesNotContain("Description = clonedDetail.Txt_Description", runtimeSource);
        }

        [Fact]
        public void CustomDetailHidesHotkeyPromptsCopiedFromTheVanillaDetail()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("clone.GetComponentsInChildren<Com_HotKey>(true)", runtimeSource);
            Assert.Contains("hotkey.gameObject.SetActive(false)", runtimeSource);
        }

        [Fact]
        public void CustomDetailRestoresTheVanillaDetailRootWhenLeaving()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("_originalDetailActive", runtimeSource);
            Assert.Contains("research.m_Tech_RPInfo.gameObject.SetActive(false)", runtimeSource);
            Assert.Contains("research.m_Tech_RPInfo.gameObject.SetActive(_originalDetailActive)", runtimeSource);
            Assert.DoesNotContain("GameMgr.Instance?._BuildMidUI", runtimeSource);
        }

        [Fact]
        public void CustomViewHidesAndRestoresTheVanillaCategoryStrip()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("_originalCategoryGroupCaptured", runtimeSource);
            Assert.Contains("_originalCategoryGroupActive", runtimeSource);
            Assert.Contains("research.Obj_CategoryGroup.SetActive(false)", runtimeSource);
            Assert.Contains("research.Obj_CategoryGroup.SetActive(_originalCategoryGroupActive)", runtimeSource);
            Assert.Contains("research.Obj_CategoryGroup.activeSelf", runtimeSource);
            Assert.DoesNotContain("research.Obj_CategoryGroup.SetActive(true)", runtimeSource);
        }

        [Fact]
        public void VanillaResearchGraphRedrawReassertsCustomViewWithoutBlockingUpdate()
        {
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var patchSource = File.ReadAllText(patchPath);
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("nameof(ResearchUI.MakeNodeGraph)", patchSource);
            Assert.Contains("ResearchUiRuntime.ReassertCustomView(__instance)", patchSource);
            Assert.DoesNotContain("[HarmonyPatch(typeof(ResearchUI), \"Update\")]", patchSource);
            Assert.DoesNotContain("TryReassertCustomUpdate", runtimeSource);
            Assert.DoesNotContain("research.enabled = false", runtimeSource);
        }

        [Fact]
        public void VanillaRightPageCannotOverwriteTheIndependentCustomDetail()
        {
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "ResearchUiPatches.cs");
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var patchSource = File.ReadAllText(patchPath);
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.DoesNotContain("nameof(Tech_RPInfo.RightPageSet)", patchSource);
            Assert.DoesNotContain("ResearchUiRuntime.ReassertCustomDetail(__instance)", patchSource);
            Assert.Contains("private static void ReassertCustomDetail()", runtimeSource);
            Assert.Contains("ShowSelectionPrompt(_research)", runtimeSource);
        }

        [Fact]
        public void CustomViewGuardReassertsAfterParentUiIsReenabled()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("rootObject.AddComponent<MultipurposeResearchViewGuard>()", runtimeSource);
            Assert.Contains("private void OnEnable()", runtimeSource);
            Assert.Contains("private void LateUpdate()", runtimeSource);
            Assert.Contains("ResearchUiRuntime.ReassertCustomView()", runtimeSource);
        }

        [Fact]
        public void CustomRootIsRegisteredBeforeItsGuardCanRunOnEnable()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            var registerRoot = runtimeSource.IndexOf("_customContentRoot = root;", System.StringComparison.Ordinal);
            var attachGuard = runtimeSource.IndexOf(
                "rootObject.AddComponent<MultipurposeResearchViewGuard>()",
                System.StringComparison.Ordinal);

            Assert.True(registerRoot >= 0);
            Assert.True(attachGuard >= 0);
            Assert.True(registerRoot < attachGuard);
        }

        [Fact]
        public void CustomViewRestoresOnlyTheRootsItTemporarilyOwns()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("_originalContentActive", runtimeSource);
            Assert.Contains("_originalDetailActive", runtimeSource);
            Assert.Contains("DestroyCustomDetailView()", runtimeSource);
            Assert.DoesNotContain("_originalBuildMidActive", runtimeSource);
            Assert.DoesNotContain("_originalDetailDescription", runtimeSource);
        }

        [Fact]
        public void ProductivityReferenceProvidesNonEmptyDisplayMetadata()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "CitizenProductivityRuntime.cs");
            var patchPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Patches", "CitizenBuffDisplayPatches.cs");
            var runtimeSource = File.ReadAllText(runtimePath);
            var patchSource = File.ReadAllText(patchPath);

            Assert.Contains("LogToAllRef", patchSource);
            Assert.Contains("T_Name", runtimeSource);
            Assert.Contains("Icon_Address", runtimeSource);
            Assert.Contains("GetEffectScript", runtimeSource);
        }

        [Fact]
        public void ReleaseVersionIsConsistentAcrossProjectPackageAndReadme()
        {
            var project = File.ReadAllText(Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "MultipurposeResearch.csproj"));
            var package = File.ReadAllText(Path.Combine(ProjectRoot(), "scripts", "Package.ps1"));
            var readme = File.ReadAllText(Path.Combine(ProjectRoot(), "README.md"));

            Assert.Contains("<Version>0.1.11</Version>", project);
            Assert.Contains("多用途研究点-v0.1.11-BepInEx5.zip", package);
            Assert.Contains("多用途研究点 v0.1.11", readme);
        }

        [Fact]
        public void ExistingLegacyCostDefaultsAreMigratedOnlyOnce()
        {
            var pluginPath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Plugin.cs");
            var source = File.ReadAllText(pluginPath);

            Assert.Contains("\"Compatibility\", \"CostDefaultsVersion\", 0", source);
            Assert.Contains("MultipurposeResearchConfig.MigrateLegacyCostDefaults(config)", source);
            Assert.Contains("productivityCost.Value = config.ProductivityCost", source);
            Assert.Contains("saleCost.Value = config.SaleCost", source);
            Assert.Contains("queenBaseCost.Value = config.QueenUpgradeBaseCost", source);
            Assert.Contains("queenCostStep.Value = config.QueenUpgradeCostStep", source);
            Assert.Contains("Config.Save()", source);
        }

        [Fact]
        public void ProductivityApplicationLogsAppliedAndFailedCounts()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "CitizenProductivityRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("appliedCount", runtimeSource);
            Assert.Contains("failedCount", runtimeSource);
        }

        [Fact]
        public void ProductivityRefreshReplacesExistingReferenceBeforeApplying()
        {
            using (var module = ModuleDefinition.ReadModule(BuiltPluginPath()))
            {
                var runtime = module.Types.Single(type =>
                    type.FullName == "MultipurposeResearch.Runtime.CitizenProductivityRuntime");
                var method = runtime.Methods.Single(item => item.Name == "ReplaceProductivityBuff");
                var refKill = method.Body.Instructions.Single(item =>
                    item.Operand is MethodReference reference && reference.Name == "RefKill");
                var buffSet = method.Body.Instructions.Single(item =>
                    item.Operand is MethodReference reference && reference.Name == "BuffRefSet");

                Assert.True(refKill.Offset < buffSet.Offset);
            }
        }

        [Fact]
        public void CountryNodesUseAnIndependentIndexAfterAllThreeQueenAttributes()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("var countryIndex = 0", runtimeSource);
        }

        [Fact]
        public void CountryNodesUseASeparateSixColumnRegionThatFitsSeventeenCountries()
        {
            var runtimePath = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "Runtime", "ResearchUiRuntime.cs");
            var runtimeSource = File.ReadAllText(runtimePath);

            Assert.Contains("private const int CountryColumnCount = 6", runtimeSource);
            Assert.Contains("GetCountryNodePosition(index, regionItemCount)", runtimeSource);
            Assert.Contains("var row = countryIndex / CountryColumnCount", runtimeSource);
            Assert.Contains("var rowItemCount = Math.Min(CountryColumnCount, countryCount - rowStartIndex)", runtimeSource);
            Assert.Contains("region == NodeRegion.Country", runtimeSource);
        }

        private static string ProjectRoot()
        {
            return typeof(PluginContractTests).Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
                .Cast<System.Reflection.AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "ProjectRoot")
                .Value;
        }

        private static string BuiltPluginPath()
        {
            var configuration = typeof(PluginContractTests).Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
                .Cast<System.Reflection.AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "BuildConfiguration")
                .Value;
            var path = Path.Combine(ProjectRoot(), "src", "MultipurposeResearch", "bin", configuration, "net472", "MultipurposeResearch.dll");
            Assert.True(File.Exists(path), $"Built plugin not found: {path}");
            return path;
        }
    }
}
