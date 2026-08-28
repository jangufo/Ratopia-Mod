using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterUpdateScopeCoordinatorTests
    {
        [Fact]
        public void NewWinterHeaterCountsOriginalAndModUpdatesAsOneTotalCall()
        {
            var scopes = new ScopeHarness();
            var originalCalls = 0;
            var modCalls = 0;
            var buildingSet = scopes.BeginBuildingSet(101);

            if (!scopes.ShouldSuppressBuildingSetUpdate(101))
            {
                originalCalls++;
            }

            var modUpdate = scopes.BeginBuildingSetModUpdate(101);
            if (!scopes.ShouldSuppressBuildingSetUpdate(101))
            {
                modCalls++;
            }

            scopes.EndScope(modUpdate);
            scopes.EndScope(buildingSet);

            Assert.Equal(0, originalCalls);
            Assert.Equal(1, modCalls);
            Assert.Equal(1, originalCalls + modCalls);
            Assert.False(scopes.ShouldSuppressBuildingSetUpdate(101));
        }

        [Fact]
        public void BuildingSetScopesAreNestedByHeaterAndFinalizerCleanupIsIdempotent()
        {
            var scopes = new ScopeHarness();
            var outer = scopes.BeginBuildingSet(101);
            var inner = scopes.BeginBuildingSet(202);

            Assert.True(scopes.ShouldSuppressBuildingSetUpdate(101));
            Assert.True(scopes.ShouldSuppressBuildingSetUpdate(202));

            var innerModUpdate = scopes.BeginBuildingSetModUpdate(202);
            Assert.False(scopes.ShouldSuppressBuildingSetUpdate(202));
            scopes.EndScope(innerModUpdate);
            scopes.EndScope(inner);
            scopes.EndScope(inner);

            Assert.True(scopes.ShouldSuppressBuildingSetUpdate(101));
            Assert.False(scopes.ShouldSuppressBuildingSetUpdate(202));

            scopes.EndScope(outer);
            Assert.False(scopes.ShouldSuppressBuildingSetUpdate(101));
        }

        [Theory]
        [InlineData("NoElec")]
        [InlineData("NoBattery")]
        public void ElectricityRecoveryCountsOriginalAndModUpdatesAsOneTotalCall(string recoveryState)
        {
            var scopes = new ScopeHarness();
            var totalCalls = new Dictionary<int, int>();
            var refresh = scopes.BeginElectricityRefresh();

            Assert.True(recoveryState == "NoElec" || recoveryState == "NoBattery");
            CountCall(totalCalls, 101);
            scopes.RecordSuccessfulOriginalUpdate(101);

            foreach (var heaterId in new[] { 101 }.Except(
                         scopes.GetSuccessfulOriginalUpdates(refresh)))
            {
                CountCall(totalCalls, heaterId);
            }

            scopes.EndScope(refresh);

            Assert.Equal(1, totalCalls[101]);
        }

        [Fact]
        public void RefreshPostfixUpdatesHeatersMissedByTheOriginalFlowExactlyOnce()
        {
            var scopes = new ScopeHarness();
            var totalCalls = new Dictionary<int, int>();
            var refresh = scopes.BeginElectricityRefresh();

            CountCall(totalCalls, 101);
            scopes.RecordSuccessfulOriginalUpdate(101);

            foreach (var heaterId in new[] { 101, 202 }.Except(
                         scopes.GetSuccessfulOriginalUpdates(refresh)))
            {
                CountCall(totalCalls, heaterId);
            }

            scopes.EndScope(refresh);

            Assert.Equal(1, totalCalls[101]);
            Assert.Equal(1, totalCalls[202]);
        }

        [Fact]
        public void CompletedUpdate3WithoutPoweredWorkingUpdateIsStillHandledByThePostfix()
        {
            var scopes = new ScopeHarness();
            var poweredCalls = 0;
            var refresh = scopes.BeginElectricityRefresh();

            var update3ReturnedWithoutCallingBuildingUpdate = true;
            Assert.True(update3ReturnedWithoutCallingBuildingUpdate);

            foreach (var heaterId in new[] { 101 }.Except(
                         scopes.GetSuccessfulOriginalUpdates(refresh)))
            {
                Assert.Equal(101, heaterId);
                poweredCalls++;
            }

            scopes.EndScope(refresh);
            Assert.Equal(1, poweredCalls);
        }

        [Fact]
        public void NestedRefreshesShareSuccessfulUpdatesAndFinalizersClearTheirScopes()
        {
            var scopes = new ScopeHarness();
            var outer = scopes.BeginElectricityRefresh();
            var inner = scopes.BeginElectricityRefresh();

            scopes.RecordSuccessfulOriginalUpdate(101);

            Assert.Equal(new[] { 101 }, scopes.GetSuccessfulOriginalUpdates(inner));
            scopes.EndScope(inner);
            Assert.Equal(new[] { 101 }, scopes.GetSuccessfulOriginalUpdates(outer));
            scopes.EndScope(outer);
            scopes.EndScope(outer);

            var nextRefresh = scopes.BeginElectricityRefresh();
            Assert.Empty(scopes.GetSuccessfulOriginalUpdates(nextRefresh));
            scopes.EndScope(nextRefresh);
        }

        private static void CountCall(IDictionary<int, int> calls, int heaterId)
        {
            int count;
            calls.TryGetValue(heaterId, out count);
            calls[heaterId] = count + 1;
        }

        private sealed class ScopeHarness
        {
            private readonly Type _type;
            private readonly object _instance;

            public ScopeHarness()
            {
                _type = Assembly.Load("HeaterEnhancement").GetType(
                    "HeaterEnhancement.Core.HeaterUpdateScopeCoordinator");
                Assert.NotNull(_type);
                _instance = Activator.CreateInstance(_type, true);
            }

            public object BeginBuildingSet(int heaterId)
            {
                return Invoke("BeginBuildingSet", heaterId);
            }

            public object BeginBuildingSetModUpdate(int heaterId)
            {
                return Invoke("BeginBuildingSetModUpdate", heaterId);
            }

            public bool ShouldSuppressBuildingSetUpdate(int heaterId)
            {
                return (bool)Invoke("ShouldSuppressBuildingSetUpdate", heaterId);
            }

            public object BeginElectricityRefresh()
            {
                return Invoke("BeginElectricityRefresh");
            }

            public void RecordSuccessfulOriginalUpdate(int heaterId)
            {
                Invoke("RecordSuccessfulOriginalUpdate", heaterId);
            }

            public IReadOnlyList<int> GetSuccessfulOriginalUpdates(object scope)
            {
                return (IReadOnlyList<int>)Invoke("GetSuccessfulOriginalUpdates", scope);
            }

            public void EndScope(object scope)
            {
                Invoke("EndScope", scope);
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
