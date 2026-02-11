using OSRSTools.Core.ValueObjects;
using Xunit;

namespace OSRSTools.UnitTests.Core.ValueObjects;

public class PriceRecommendationTests
{
    [Fact]
    public void HasSufficientData_BothWindowsAtLeast2_ReturnsTrue()
    {
        var rec = new PriceRecommendation
        {
            RecommendedBuyPrice = 100,
            RecommendedSellPrice = 120,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 3
        };

        Assert.True(rec.HasSufficientData);
    }

    [Fact]
    public void HasSufficientData_BuyWindowsBelow2_ReturnsFalse()
    {
        var rec = new PriceRecommendation
        {
            WindowsUsedForBuy = 1,
            WindowsUsedForSell = 3
        };

        Assert.False(rec.HasSufficientData);
    }

    [Fact]
    public void HasSufficientData_SellWindowsBelow2_ReturnsFalse()
    {
        var rec = new PriceRecommendation
        {
            WindowsUsedForBuy = 4,
            WindowsUsedForSell = 1
        };

        Assert.False(rec.HasSufficientData);
    }

    [Fact]
    public void HasSufficientData_BothBelow2_ReturnsFalse()
    {
        var rec = new PriceRecommendation
        {
            WindowsUsedForBuy = 0,
            WindowsUsedForSell = 0
        };

        Assert.False(rec.HasSufficientData);
    }

    [Fact]
    public void GrossMargin_CalculatesCorrectly()
    {
        var rec = new PriceRecommendation
        {
            RecommendedBuyPrice = 100,
            RecommendedSellPrice = 150
        };

        Assert.Equal(50, rec.GrossMargin);
    }

    [Fact]
    public void GrossMargin_NegativeMargin_ReturnsNegative()
    {
        var rec = new PriceRecommendation
        {
            RecommendedBuyPrice = 200,
            RecommendedSellPrice = 150
        };

        Assert.Equal(-50, rec.GrossMargin);
    }
}
