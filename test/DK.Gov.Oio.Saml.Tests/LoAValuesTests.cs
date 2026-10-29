using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Tests;

public class LoAValuesTests
{
    [Theory]
    [InlineData(LoAValues.Low, 0)]
    [InlineData(LoAValues.Substantial, 1)]
    [InlineData(LoAValues.High, 2)]
    [InlineData("http://eidas.europa.eu/LoA/low", 0)]
    [InlineData("http://eidas.europa.eu/LoA/substantial", 1)]
    [InlineData("http://eidas.europa.eu/LoA/high", 2)]
    [InlineData("Medium", -1)]
    [InlineData("http://eidas.europa.eu/LoA/medium", -1)]
    [InlineData("substantial", -1)]
    [InlineData(null, -1)]
    public void NsisAndEidasValues_AreRanked(string? value, int rank) => Assert.Equal(rank, LoAValues.GetLoARank(value));
}
