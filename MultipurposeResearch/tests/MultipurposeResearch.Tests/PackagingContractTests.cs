using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MultipurposeResearch.Tests
{
    public sealed class PackagingContractTests
    {
        [Fact]
        public void ReadmeExplainsVanillaAssetReuseAndSaveRemoval()
        {
            var path = Path.Combine(ProjectRoot(), "README.md");
            Assert.True(File.Exists(path));
            var text = File.ReadAllText(path);
            Assert.Contains("复用游戏内资源", text);
            Assert.Contains("ModsData", text);
            Assert.Contains("移除", text);
        }

        private static string ProjectRoot()
        {
            var root = typeof(PackagingContractTests).Assembly
                .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
                .Cast<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "ProjectRoot")
                .Value;
            Assert.True(Directory.Exists(root));
            return root;
        }
    }
}
