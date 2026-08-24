using HeaterEnhancement.Core;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterCoverageRegistryTests
    {
        [Fact]
        public void RegisteringTheSameHeaterReplacesItsOldCoverage()
        {
            var registry = new HeaterCoverageRegistry();
            var oldPoint = new GridPoint(1, 1);
            var currentPoint = new GridPoint(2, 1);

            registry.Register(100, new[] { oldPoint });
            registry.Register(100, new[] { currentPoint });

            Assert.False(registry.Covers(oldPoint));
            Assert.True(registry.Covers(currentPoint));
        }

        [Fact]
        public void UnregisterAndClearRemoveOnlyTheRequestedCoverage()
        {
            var registry = new HeaterCoverageRegistry();
            var first = new GridPoint(3, 3);
            var second = new GridPoint(4, 3);

            registry.Register(100, new[] { first });
            registry.Register(200, new[] { second });
            registry.Unregister(100);

            Assert.False(registry.Covers(first));
            Assert.True(registry.Covers(second));

            registry.Clear();
            Assert.False(registry.Covers(second));
        }

        [Fact]
        public void UnregisteringOneOverlappingHeaterKeepsTheOtherCoverage()
        {
            var registry = new HeaterCoverageRegistry();
            var shared = new GridPoint(6, 6);

            registry.Register(100, new[] { shared });
            registry.Register(200, new[] { shared });
            registry.Unregister(100);

            Assert.True(registry.Covers(shared));
            registry.Unregister(200);
            Assert.False(registry.Covers(shared));
        }

        [Fact]
        public void ReplacingAndRemovingCoverageReturnsOnlyPointsNoHeaterStillCovers()
        {
            var registry = new HeaterCoverageRegistry();
            var oldOnly = new GridPoint(1, 1);
            var shared = new GridPoint(2, 1);
            var newOnly = new GridPoint(3, 1);
            registry.Register(100, new[] { oldOnly, shared });
            registry.Register(200, new[] { shared });

            var register = typeof(HeaterCoverageRegistry).GetMethod("Register");
            var unregister = typeof(HeaterCoverageRegistry).GetMethod("Unregister");
            Assert.NotNull(register);
            Assert.NotNull(unregister);
            Assert.NotEqual(typeof(void), register.ReturnType);
            Assert.NotEqual(typeof(void), unregister.ReturnType);

            var replaced = Assert.IsAssignableFrom<IReadOnlyList<GridPoint>>(
                register.Invoke(registry, new object[] { 100, new[] { shared, newOnly } }));
            Assert.Equal(new[] { oldOnly }, replaced);

            var removedFirst = Assert.IsAssignableFrom<IReadOnlyList<GridPoint>>(
                unregister.Invoke(registry, new object[] { 100 }));
            Assert.Equal(new[] { newOnly }, removedFirst);

            var removedLast = Assert.IsAssignableFrom<IReadOnlyList<GridPoint>>(
                unregister.Invoke(registry, new object[] { 200 }));
            Assert.Equal(new[] { shared }, removedLast);
        }

        [Fact]
        public void CoversAnyRequiresAtLeastOneRegisteredOccupiedPoint()
        {
            var registry = new HeaterCoverageRegistry();
            var covered = new GridPoint(4, 5);
            registry.Register(100, new[] { covered });

            var coversAny = typeof(HeaterCoverageRegistry).GetMethod(
                "CoversAny",
                new[] { typeof(IEnumerable<GridPoint>) });
            Assert.NotNull(coversAny);
            Assert.Equal(typeof(bool), coversAny.ReturnType);

            Assert.False((bool)coversAny.Invoke(registry, new object[] { null }));
            Assert.False((bool)coversAny.Invoke(
                registry,
                new object[] { new GridPoint[0] }));
            Assert.False((bool)coversAny.Invoke(
                registry,
                new object[] { new[] { new GridPoint(20, 20), new GridPoint(21, 20) } }));
            Assert.True((bool)coversAny.Invoke(
                registry,
                new object[] { new[] { new GridPoint(20, 20), covered } }));
        }
    }
}
