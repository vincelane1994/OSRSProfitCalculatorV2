using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Services;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class PriceRecommendationServiceTests
{
    private readonly PriceRecommendationService _sut;

    public PriceRecommendationServiceTests()
    {
        var priceWeights = Options.Create(new PriceWeightSettings
        {
            FiveMinute = 0.10,
            OneHour = 0.35,
            SixHour = 0.35,
            TwentyFourHour = 0.20
        });
        _sut = new PriceRecommendationService(priceWeights);
    }

    #region CalculateRecommendedPrices

    [Fact]
    public void CalculateRecommendedPrices_AllWindowsAvailable_ReturnsWeightedAverage()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 108, AvgSellPrice = 93 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(4, result.WindowsUsedForBuy);
        Assert.Equal(4, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);
        Assert.True(result.RecommendedSellPrice > 0);
        Assert.True(result.RecommendedBuyPrice > 0);
        Assert.True(result.GrossMargin > 0);

        // Verify weighted averages are calculated correctly
        // Buy price = weighted avg of AvgSellPrice: 0.1*90 + 0.35*95 + 0.35*92 + 0.2*93 = 93.05 ≈ 93
        Assert.Equal(93, result.RecommendedBuyPrice);

        // Sell price = weighted avg of AvgBuyPrice: 0.1*100 + 0.35*110 + 0.35*105 + 0.2*108 = 107.35 ≈ 107
        Assert.Equal(107, result.RecommendedSellPrice);

        // Gross margin = 107 - 93 = 14
        Assert.Equal(14, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_MissingWindows_RedistributesWeight()
    {
        // Arrange - only OneHour and SixHour available
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(2, result.WindowsUsedForBuy);
        Assert.Equal(2, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);

        // Weights: OneHour=0.35, SixHour=0.35, total=0.70
        // Redistributed: OneHour=0.35/0.70=0.5, SixHour=0.35/0.70=0.5
        // Buy price = 0.5*90 + 0.5*95 = 92.5 ≈ 92 (banker's rounding)
        Assert.Equal(92, result.RecommendedBuyPrice);

        // Sell price = 0.5*100 + 0.5*110 = 105
        Assert.Equal(105, result.RecommendedSellPrice);

        // Gross margin = 105 - 92 = 13
        Assert.Equal(13, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_OnlyOneWindow_InsufficientData()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(1, result.WindowsUsedForBuy);
        Assert.Equal(1, result.WindowsUsedForSell);
        Assert.False(result.HasSufficientData);

        // Still calculates prices even with one window
        Assert.Equal(90, result.RecommendedBuyPrice);
        Assert.Equal(100, result.RecommendedSellPrice);
        Assert.Equal(10, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_NoWindows_ReturnsZero()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>()
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(0, result.RecommendedSellPrice);
        Assert.Equal(0, result.WindowsUsedForBuy);
        Assert.Equal(0, result.WindowsUsedForSell);
        Assert.False(result.HasSufficientData);
        Assert.Equal(0, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_AllPricesNull_ReturnsZero()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = null, AvgSellPrice = null },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = null, AvgSellPrice = null },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = null, AvgSellPrice = null },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = null, AvgSellPrice = null }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(0, result.RecommendedSellPrice);
        Assert.Equal(0, result.WindowsUsedForBuy);
        Assert.Equal(0, result.WindowsUsedForSell);
        Assert.False(result.HasSufficientData);
    }

    [Fact]
    public void CalculateRecommendedPrices_NullPricesInWindows_ExcludesThoseWindows()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = null, AvgSellPrice = null },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(3, result.WindowsUsedForBuy);
        Assert.Equal(3, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);

        // Weights: OneHour=0.35, SixHour=0.35, TwentyFourHour=0.20, total=0.90
        // Redistributed: OneHour=0.35/0.90≈0.389, SixHour=0.35/0.90≈0.389, TwentyFourHour=0.20/0.90≈0.222
        // Buy price = 0.389*90 + 0.389*95 + 0.222*92 ≈ 92.388 ≈ 92
        Assert.Equal(92, result.RecommendedBuyPrice);

        // Sell price = 0.389*100 + 0.389*110 + 0.222*105 ≈ 105
        Assert.Equal(105, result.RecommendedSellPrice);
    }

    [Fact]
    public void CalculateRecommendedPrices_ZeroPricesTreatedAsUnavailable_ExcludesThoseWindows()
    {
        // Arrange - zero prices should be excluded
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 0, AvgSellPrice = 0 },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert - zero prices should be excluded, leaving 3 windows
        Assert.Equal(3, result.WindowsUsedForBuy);
        Assert.Equal(3, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);

        // Should have same result as NullPricesInWindows test
        Assert.Equal(92, result.RecommendedBuyPrice);
        Assert.Equal(105, result.RecommendedSellPrice);
    }

    [Fact]
    public void CalculateRecommendedPrices_PartialNullPrices_HandlesIndependently()
    {
        // Arrange - some windows have only buy or only sell prices
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 100, AvgSellPrice = null },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = null, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert - buy and sell windows used independently
        Assert.Equal(3, result.WindowsUsedForBuy); // OneHour, SixHour, TwentyFourHour have sell prices
        Assert.Equal(3, result.WindowsUsedForSell); // FiveMinute, OneHour, TwentyFourHour have buy prices
        Assert.True(result.HasSufficientData);
        Assert.True(result.RecommendedBuyPrice > 0);
        Assert.True(result.RecommendedSellPrice > 0);
    }

    [Fact]
    public void CalculateRecommendedPrices_CustomWeights_UsesProvidedConfiguration()
    {
        // Arrange - create service with custom weights
        var customWeights = Options.Create(new PriceWeightSettings
        {
            FiveMinute = 0.50, // Heavy recent bias
            OneHour = 0.30,
            SixHour = 0.15,
            TwentyFourHour = 0.05
        });
        var customSut = new PriceRecommendationService(customWeights);

        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 108, AvgSellPrice = 93 }
            }
        };

        // Act
        var result = customSut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(4, result.WindowsUsedForBuy);
        Assert.Equal(4, result.WindowsUsedForSell);

        // Buy price = 0.5*90 + 0.3*95 + 0.15*92 + 0.05*93 = 92.0
        Assert.Equal(92, result.RecommendedBuyPrice);

        // Sell price = 0.5*100 + 0.3*110 + 0.15*105 + 0.05*108 = 104.15 ≈ 104
        Assert.Equal(104, result.RecommendedSellPrice);

        // Gross margin = 104 - 92 = 12
        Assert.Equal(12, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_HasSufficientData_RequiresTwoOrMoreWindows()
    {
        // Arrange - test boundary cases for HasSufficientData
        var priceDataZero = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>()
        };

        var priceDataOne = new ItemPriceData
        {
            ItemId = 2,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var priceDataTwo = new ItemPriceData
        {
            ItemId = 3,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 }
            }
        };

        // Act
        var resultZero = _sut.CalculateRecommendedPrices(priceDataZero);
        var resultOne = _sut.CalculateRecommendedPrices(priceDataOne);
        var resultTwo = _sut.CalculateRecommendedPrices(priceDataTwo);

        // Assert
        Assert.False(resultZero.HasSufficientData); // 0 windows
        Assert.False(resultOne.HasSufficientData);  // 1 window
        Assert.True(resultTwo.HasSufficientData);   // 2 windows
    }

    [Fact]
    public void CalculateRecommendedPrices_GrossMargin_CalculatesCorrectly()
    {
        // Arrange
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 1000, AvgSellPrice = 900 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 1100, AvgSellPrice = 950 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        // Buy = 0.5*900 + 0.5*950 = 925
        // Sell = 0.5*1000 + 0.5*1100 = 1050
        // Margin = 1050 - 925 = 125
        Assert.Equal(925, result.RecommendedBuyPrice);
        Assert.Equal(1050, result.RecommendedSellPrice);
        Assert.Equal(125, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_NegativeMargin_AllowedByCalculation()
    {
        // Arrange - sell price lower than buy price (inverted market)
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 110 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 95, AvgSellPrice = 105 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        // Buy = 0.5*110 + 0.5*105 = 107.5 ≈ 108
        // Sell = 0.5*100 + 0.5*95 = 97.5 ≈ 98
        // Margin = 98 - 108 = -10 (negative)
        Assert.Equal(108, result.RecommendedBuyPrice);
        Assert.Equal(98, result.RecommendedSellPrice);
        Assert.Equal(-10, result.GrossMargin);
    }

    [Fact]
    public void CalculateRecommendedPrices_LargePriceValues_HandlesCorrectly()
    {
        // Arrange - test with large item prices (e.g., Twisted Bow)
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 1_000_000_000, AvgSellPrice = 990_000_000 },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 1_010_000_000, AvgSellPrice = 995_000_000 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 1_005_000_000, AvgSellPrice = 992_000_000 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 1_008_000_000, AvgSellPrice = 993_000_000 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(4, result.WindowsUsedForBuy);
        Assert.Equal(4, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);

        // Verify prices are in expected range (around 990M-1010M)
        Assert.True(result.RecommendedBuyPrice > 900_000_000);
        Assert.True(result.RecommendedBuyPrice < 1_000_000_000);
        Assert.True(result.RecommendedSellPrice > 1_000_000_000);
        Assert.True(result.RecommendedSellPrice < 1_100_000_000);
        Assert.True(result.GrossMargin > 0);
    }

    [Fact]
    public void CalculateRecommendedPrices_OnlyBuyPricesAvailable_SellPriceZero()
    {
        // Arrange - only AvgBuyPrice populated
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = null },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = null }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(0, result.WindowsUsedForBuy); // No sell prices for buy calculation
        Assert.Equal(2, result.WindowsUsedForSell); // Buy prices available for sell calculation
        Assert.False(result.HasSufficientData);
        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(105, result.RecommendedSellPrice); // (100+110)/2
    }

    [Fact]
    public void CalculateRecommendedPrices_OnlySellPricesAvailable_BuyPriceZero()
    {
        // Arrange - only AvgSellPrice populated
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = null, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = null, AvgSellPrice = 95 }
            }
        };

        // Act
        var result = _sut.CalculateRecommendedPrices(priceData);

        // Assert
        Assert.Equal(2, result.WindowsUsedForBuy); // Sell prices available for buy calculation
        Assert.Equal(0, result.WindowsUsedForSell); // No buy prices for sell calculation
        Assert.False(result.HasSufficientData);
        Assert.Equal(92, result.RecommendedBuyPrice); // 0.5*90 + 0.5*95 = 92.5 ≈ 92
        Assert.Equal(0, result.RecommendedSellPrice);
    }

    #endregion
}
