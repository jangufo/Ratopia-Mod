using System.Collections.Generic;
using HeaterEnhancement.Core;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class PlantSaveBuffSanitizerTests
    {
        [Fact]
        public void RemovesMatchingHeatSystemNameAndValueAtTheSameIndex()
        {
            var names = new List<string> { "Rain", "HeatSystem", "Sun" };
            var values = new List<float> { 1f, 0f, 2f };

            PlantSaveBuffSanitizer.RemoveHeatSystemPairs(names, values, "HeatSystem");

            Assert.Equal(new[] { "Rain", "Sun" }, names);
            Assert.Equal(new[] { 1f, 2f }, values);
        }

        [Fact]
        public void DoesNotDeleteAnUnpairedNameWhenTheListsAreUneven()
        {
            var names = new List<string> { "Rain", "HeatSystem" };
            var values = new List<float> { 1f };

            PlantSaveBuffSanitizer.RemoveHeatSystemPairs(names, values, "HeatSystem");

            Assert.Equal(new[] { "Rain", "HeatSystem" }, names);
            Assert.Equal(new[] { 1f }, values);
        }
    }
}
