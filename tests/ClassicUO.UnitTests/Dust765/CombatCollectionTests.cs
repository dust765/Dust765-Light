using FluentAssertions;
using Xunit;
using ClassicUO.Dust765;

namespace ClassicUO.UnitTests.Dust765
{
    public class CombatCollectionTests
    {
        [Theory]
        [InlineData(0x1E03)]
        [InlineData(0x1E04)]
        [InlineData(0x1E05)]
        [InlineData(0x1E06)]
        public void IsStealthArt_Returns_True_For_Stealth_Graphics(ushort graphic)
        {
            CombatCollection.IsStealthArt(graphic).Should().BeTrue();
        }

        [Theory]
        [InlineData(0x0000)]
        [InlineData(0x1E02)]
        [InlineData(0x1E07)]
        [InlineData(0x0190)]
        public void IsStealthArt_Returns_False_For_Non_Stealth_Graphics(ushort graphic)
        {
            CombatCollection.IsStealthArt(graphic).Should().BeFalse();
        }
    }
}
