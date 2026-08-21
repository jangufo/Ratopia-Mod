using System.Reflection;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class InitialContractTests
    {
        [Fact]
        public void HeaterRangeCalculatorIsAvailableAsPureLogic()
        {
            var assembly = Assembly.Load("HeaterEnhancement");
            var calculator = assembly.GetType("HeaterEnhancement.Core.HeaterRangeCalculator");

            Assert.NotNull(calculator);
            Assert.NotNull(calculator.GetMethod("CreateCoverage"));
        }
    }
}
