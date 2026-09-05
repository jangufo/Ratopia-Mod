using System;
using SpecialRatizens.Core;
using Xunit;

namespace SpecialRatizens.Tests
{
    public sealed class SpecialNamePolicyTests
    {
        [Fact]
        public void TreatsColoredAndUncoloredNamesAsTheSameName()
        {
            Assert.True(SpecialNamePolicy.IsTaken("YunQing", new[] { "<color=#ffffff>YunQing</color>" }));
        }

        [Fact]
        public void DoesNotTreatDifferentNamesAsDuplicates()
        {
            Assert.False(SpecialNamePolicy.IsTaken("YunQing", new[] { "YunQing2" }));
        }

        [Fact]
        public void IgnoresBlankCandidateNames()
        {
            Assert.False(SpecialNamePolicy.IsTaken("  ", new[] { "<color=#ffffff></color>" }));
        }
    }
}
