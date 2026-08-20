using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using HeaterEnhancement.Core;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class PackagingContractTests
    {
        [Fact]
        public void ProjectReferencesAreNeverCopiedBesideThePlugin()
        {
            var project = File.ReadAllText(ProjectPath("src", "HeaterEnhancement", "HeaterEnhancement.csproj"));

            Assert.Equal(6, CountOccurrences(project, "Private=\"false\""));
            Assert.DoesNotContain("<Private>true</Private>", project);
        }

        [Fact]
        public void ReleaseOutputContainsOnlyThePluginDll()
        {
            var output = ProjectPath("src", "HeaterEnhancement", "bin", "Release", "net472");
            Assert.True(Directory.Exists(output), $"Release output not found: {output}");

            var outputFileNames = Directory.GetFiles(output)
                .Select(Path.GetFileName)
                .OrderBy(name => name)
                .ToArray();
            Assert.Equal(new[] { "HeaterEnhancement.dll" }, outputFileNames);

            using (var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(output, "HeaterEnhancement.dll")))
            {
                Assert.Equal(new Version(0, 1, 3, 0), assembly.Name.Version);
            }
        }

        [Fact]
        public void PackageScriptBuildsWithoutInstallingAndUsesTheExactLayout()
        {
            var script = RequireScript("Package.ps1");

            Assert.Contains("InstallAfterBuild=false", script);
            Assert.Contains("加热器加强优化-v0.1.3-BepInEx5.zip", script);
            Assert.Contains("BepInEx\\plugins\\HeaterEnhancement", script);
            Assert.Contains("HeaterEnhancement.dll", script);
            Assert.Contains("README.md", script);
            Assert.Contains("Test-RatopiaPackage.ps1", script);
            Assert.Contains("$actualSignature", script);
            Assert.Contains("$expectedSignature", script);
        }

        [Fact]
        public void PackageScriptRejectsRuntimeDependenciesAndDebugArtifacts()
        {
            var script = RequireScript("Package.ps1");

            foreach (var forbidden in new[]
                     {
                         "Assembly-CSharp.dll",
                         "0Harmony.dll",
                         "BepInEx.dll",
                         "UnityEngine.dll",
                         "UnityEngine.CoreModule.dll",
                         "*.pdb"
                     })
            {
                Assert.Contains(forbidden, script);
            }
        }

        [Fact]
        public void InstallScriptUsesClosedGameDuplicateGuidAndAtomicReplacementGates()
        {
            var script = RequireScript("Install.ps1");

            Assert.Contains("$expectedVersion = '0.1.3'", script);

            foreach (var required in new[]
                     {
                         "Get-Process -Name 'Ratopia'",
                         "plugins",
                         "patchers",
                         "cn.ratopia.heaterenhancement",
                         ".installing",
                         "Move-Item",
                         "backups",
                         "Mono.Cecil",
                         "Get-FileHash"
                     })
            {
                Assert.Contains(required, script);
            }
        }

        private static string RequireScript(string name)
        {
            var path = ProjectPath("scripts", name);
            Assert.True(File.Exists(path), $"Missing script: {path}");
            return File.ReadAllText(path);
        }

        private static string ProjectPath(params string[] parts)
        {
            var root = typeof(PackagingContractTests).Assembly
                .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
                .Cast<AssemblyMetadataAttribute>()
                .SingleOrDefault(attribute => attribute.Key == "ProjectRoot")
                ?.Value;
            if (string.IsNullOrEmpty(root))
            {
                root = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(typeof(PackagingContractTests).Assembly.Location),
                    "..", "..", "..", "..", ".."));
            }

            var path = root;
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        private static int CountOccurrences(string value, string search)
        {
            var count = 0;
            var index = 0;
            while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += search.Length;
            }

            return count;
        }
    }
}
