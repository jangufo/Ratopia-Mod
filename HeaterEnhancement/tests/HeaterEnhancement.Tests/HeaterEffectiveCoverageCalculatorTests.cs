using System.Collections.Generic;
using System.Reflection;
using HeaterEnhancement.Core;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterEffectiveCoverageCalculatorTests
    {
        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void CoverageIsEmptyUnlessTheHeaterIsActivePoweredAndInWinter(
            bool isWinter,
            bool isActive,
            bool hasElectricity)
        {
            var coverage = Create(
                new[] { new GridPoint(1, 1) },
                new GridPoint[0],
                isWinter,
                isActive,
                hasElectricity);

            Assert.Empty(coverage);
        }

        [Fact]
        public void PoweredWinterCoverageExcludesBlockedCellsAndPreservesOrder()
        {
            var first = new GridPoint(1, 1);
            var blocked = new GridPoint(2, 1);
            var last = new GridPoint(3, 1);

            var coverage = Create(
                new[] { first, blocked, last },
                new[] { blocked },
                true,
                true,
                true);

            Assert.Equal(new[] { first, last }, coverage);
        }

        [Fact]
        public void EffectiveCoverageSupportsOverlappingHeaterRegistrations()
        {
            var registry = new HeaterCoverageRegistry();
            var shared = new GridPoint(5, 5);
            registry.Register(100, Create(
                new[] { new GridPoint(4, 5), shared },
                new GridPoint[0],
                true,
                true,
                true));
            registry.Register(200, Create(
                new[] { shared, new GridPoint(6, 5) },
                new GridPoint[0],
                true,
                true,
                true));

            registry.Unregister(100);

            Assert.True(registry.Covers(shared));
        }

        private static IReadOnlyList<GridPoint> Create(
            IEnumerable<GridPoint> candidates,
            IEnumerable<GridPoint> blocked,
            bool isWinter,
            bool isActive,
            bool hasElectricity)
        {
            var calculator = typeof(HeaterCoverageRegistry).Assembly.GetType(
                "HeaterEnhancement.Core.HeaterEffectiveCoverageCalculator");
            Assert.NotNull(calculator);
            var create = calculator.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(IEnumerable<GridPoint>),
                    typeof(IEnumerable<GridPoint>),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool)
                },
                null);
            Assert.NotNull(create);

            return Assert.IsAssignableFrom<IReadOnlyList<GridPoint>>(create.Invoke(
                null,
                new object[] { candidates, blocked, isWinter, isActive, hasElectricity }));
        }
    }
}
