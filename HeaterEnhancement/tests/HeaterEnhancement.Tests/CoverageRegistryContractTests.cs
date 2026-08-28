using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class CoverageRegistryContractTests
    {
        [Fact]
        public void CoverageRegistryIsAvailableForSaveFiltering()
        {
            var assembly = Assembly.Load("HeaterEnhancement");
            var registry = assembly.GetType("HeaterEnhancement.Core.HeaterCoverageRegistry");

            Assert.NotNull(registry);
            Assert.NotNull(registry.GetMethod("Register"));
            Assert.NotNull(registry.GetMethod("Unregister"));
            Assert.NotNull(registry.GetMethod("Clear"));
        }
    }
}
