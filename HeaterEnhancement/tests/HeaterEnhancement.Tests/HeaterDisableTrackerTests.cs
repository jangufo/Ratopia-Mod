using System;
using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HeaterDisableTrackerTests
    {
        [Fact]
        public void NonWinterVisualShutdownRunsOnceUntilTheHeaterReturnsToWinter()
        {
            var type = Assembly.Load("HeaterEnhancement").GetType(
                "HeaterEnhancement.Core.HeaterDisableTracker");
            Assert.NotNull(type);

            var tracker = Activator.CreateInstance(type, true);
            var beginDisable = type.GetMethod("BeginNonWinterDisable");
            var markWinter = type.GetMethod("MarkWinter");
            var remove = type.GetMethod("Remove");
            Assert.NotNull(beginDisable);
            Assert.NotNull(markWinter);
            Assert.NotNull(remove);

            Assert.True((bool)beginDisable.Invoke(tracker, new object[] { 42 }));
            Assert.False((bool)beginDisable.Invoke(tracker, new object[] { 42 }));

            markWinter.Invoke(tracker, new object[] { 42 });
            Assert.True((bool)beginDisable.Invoke(tracker, new object[] { 42 }));

            remove.Invoke(tracker, new object[] { 42 });
            Assert.True((bool)beginDisable.Invoke(tracker, new object[] { 42 }));
        }
    }
}
