using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterSeasonBatchCoordinatorTests
    {
        [Fact]
        public void FirstDiscoveryAppliesTheHeaterOnceAndTheNextTickDoesNothing()
        {
            var coordinator = new CoordinatorHarness();
            var calls = new List<int>();

            coordinator.QueueHeater(3, 101);
            RunPending(coordinator, calls);
            Assert.True(coordinator.TryCommit(out var committedSeason));
            Assert.Equal(3, committedSeason);

            coordinator.EnsureSeasonBatch(3, new[] { 101 });
            RunPending(coordinator, calls);

            Assert.Equal(new[] { 101 }, calls);
            Assert.Empty(coordinator.PendingHeaterIds);
        }

        [Fact]
        public void SeasonPostfixAppliesEachHeaterOnceAndTheNextTickDoesNothing()
        {
            var coordinator = CreateCommittedCoordinator(2);
            var calls = new List<int>();

            coordinator.EnsureSeasonBatch(3, new[] { 101, 202 });
            RunPending(coordinator, calls);
            Assert.True(coordinator.TryCommit(out var committedSeason));
            Assert.Equal(3, committedSeason);

            coordinator.EnsureSeasonBatch(3, new[] { 101, 202 });
            RunPending(coordinator, calls);

            Assert.Equal(new[] { 101, 202 }, calls);
        }

        [Fact]
        public void ElectricityRefreshAppliesEachHeaterOnceAndTheNextTickDoesNothing()
        {
            var coordinator = CreateCommittedCoordinator(3);
            var calls = new List<int>();

            coordinator.BeginBatch(3, new[] { 101, 202 });
            RunPending(coordinator, calls);
            Assert.True(coordinator.TryCommit(out _));

            coordinator.EnsureSeasonBatch(3, new[] { 101, 202 });
            RunPending(coordinator, calls);

            Assert.Equal(new[] { 101, 202 }, calls);
        }

        [Fact]
        public void PartialFailureContinuesLaterHeatersAndRetriesOnlyTheFailure()
        {
            var coordinator = new CoordinatorHarness();
            var firstAttempt = new List<int>();
            coordinator.BeginBatch(3, new[] { 101, 202, 303 });

            RunPending(coordinator, firstAttempt, 202);

            Assert.Equal(new[] { 101, 202, 303 }, firstAttempt);
            Assert.False(coordinator.TryCommit(out _));
            Assert.Equal(new[] { 202 }, coordinator.PendingHeaterIds);

            var retry = new List<int>();
            coordinator.EnsureSeasonBatch(3, new[] { 101, 202, 303 });
            RunPending(coordinator, retry);

            Assert.Equal(new[] { 202 }, retry);
            Assert.True(coordinator.TryCommit(out var committedSeason));
            Assert.Equal(3, committedSeason);
        }

        [Fact]
        public void SeasonMarkerCommitsOnlyAfterEveryPendingHeaterSucceeds()
        {
            var coordinator = CreateCommittedCoordinator(2);
            coordinator.BeginBatch(3, new[] { 101, 202 });

            coordinator.MarkSucceeded(101);

            Assert.False(coordinator.TryCommit(out _));
            Assert.Equal(2, coordinator.LastCommittedSeason);

            coordinator.MarkSucceeded(202);

            Assert.True(coordinator.TryCommit(out var committedSeason));
            Assert.Equal(3, committedSeason);
            Assert.Equal(3, coordinator.LastCommittedSeason);
        }

        private static CoordinatorHarness CreateCommittedCoordinator(int season)
        {
            var coordinator = new CoordinatorHarness();
            coordinator.BeginBatch(season, new int[0]);
            Assert.True(coordinator.TryCommit(out _));
            return coordinator;
        }

        private static void RunPending(
            CoordinatorHarness coordinator,
            ICollection<int> calls,
            int failedHeaterId = int.MinValue)
        {
            foreach (var heaterId in coordinator.PendingHeaterIds.ToArray())
            {
                calls.Add(heaterId);
                if (heaterId != failedHeaterId)
                {
                    coordinator.MarkSucceeded(heaterId);
                }
            }
        }

        private sealed class CoordinatorHarness
        {
            private readonly Type _type;
            private readonly object _instance;

            public CoordinatorHarness()
            {
                _type = Assembly.Load("HeaterEnhancement").GetType(
                    "HeaterEnhancement.Core.HeaterSeasonBatchCoordinator");
                Assert.NotNull(_type);
                _instance = Activator.CreateInstance(_type, true);
            }

            public IReadOnlyList<int> PendingHeaterIds =>
                (IReadOnlyList<int>)Invoke("GetPendingHeaterIds");

            public int LastCommittedSeason =>
                (int)_type.GetProperty("LastCommittedSeason").GetValue(_instance);

            public void BeginBatch(int season, IEnumerable<int> heaterIds)
            {
                Invoke("BeginBatch", season, heaterIds);
            }

            public void EnsureSeasonBatch(int season, IEnumerable<int> heaterIds)
            {
                Invoke("EnsureSeasonBatch", season, heaterIds);
            }

            public void QueueHeater(int season, int heaterId)
            {
                Invoke("QueueHeater", season, heaterId);
            }

            public void MarkSucceeded(int heaterId)
            {
                Invoke("MarkSucceeded", heaterId);
            }

            public bool TryCommit(out int committedSeason)
            {
                var arguments = new object[] { 0 };
                var result = (bool)Invoke("TryCommit", arguments);
                committedSeason = (int)arguments[0];
                return result;
            }

            private object Invoke(string methodName, params object[] arguments)
            {
                var method = _type.GetMethod(methodName);
                Assert.NotNull(method);
                return method.Invoke(_instance, arguments);
            }
        }
    }
}
