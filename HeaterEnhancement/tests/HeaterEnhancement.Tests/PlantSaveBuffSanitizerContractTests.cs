using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class PlantSaveBuffSanitizerContractTests
    {
        [Fact]
        public void PlantSaveBuffSanitizerIsAvailableForCopiedSaveData()
        {
            var assembly = Assembly.Load("HeaterEnhancement");
            var sanitizer = assembly.GetType("HeaterEnhancement.Core.PlantSaveBuffSanitizer");

            Assert.NotNull(sanitizer);
            Assert.NotNull(sanitizer.GetMethod("RemoveHeatSystemPairs"));
        }
    }
}
