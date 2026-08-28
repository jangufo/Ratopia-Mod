using System.Collections.Generic;
using HeaterEnhancement.Core;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterRangeCalculatorTests
    {
        [Fact]
        public void CreateCoverageExcludesAllSixHeaterFootprintCells()
        {
            var coverage = HeaterRangeCalculator.CreateCoverage(
                new GridPoint(12, 20),
                HeaterFootprint());

            Assert.Equal(39, coverage.Count);
            foreach (var occupied in HeaterFootprint())
            {
                Assert.DoesNotContain(occupied, coverage);
            }
        }

        [Fact]
        public void CreateCoverageUsesStableTopToBottomNineByFiveCoordinates()
        {
            var coverage = HeaterRangeCalculator.CreateCoverage(
                new GridPoint(12, 20),
                new GridPoint[0]);

            Assert.Equal(45, coverage.Count);
            Assert.Equal(new GridPoint(8, 20), coverage[0]);
            Assert.Equal(new GridPoint(16, 20), coverage[8]);
            Assert.Equal(new GridPoint(8, 16), coverage[36]);
            Assert.Equal(new GridPoint(16, 16), coverage[44]);
            Assert.Equal(new GridPoint(8, 19), coverage[9]);
        }

        private static IReadOnlyList<GridPoint> HeaterFootprint()
        {
            return new[]
            {
                new GridPoint(11, 20), new GridPoint(12, 20), new GridPoint(13, 20),
                new GridPoint(11, 19), new GridPoint(12, 19), new GridPoint(13, 19)
            };
        }
    }
}
