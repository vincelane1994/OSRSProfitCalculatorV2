using OSRSTools.Core.Entities;
using OSRSTools.Core.Services;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class ManipulationDetectorTests
{
    private readonly ManipulationDetector _sut = new();

    #region Price Deviation Detection

    [Fact]
    public void IsSuspicious_NormalPrices_ReturnsFalse()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.False(result);
    }

    [Fact]
    public void IsSuspicious_HighDeviation_BuyPrice_ReturnsTrue()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 200, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.True(result);
    }

    [Fact]
    public void IsSuspicious_HighDeviation_SellPrice_ReturnsTrue()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 180 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.True(result);
    }

    [Fact]
    public void IsSuspicious_AtExactThreshold_ReturnsFalse()
    {
        // 50% deviation exactly AT threshold should not be flagged (> not >=)
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 150, AvgSellPrice = 90 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.False(result);
    }

    [Fact]
    public void IsSuspicious_Missing5mWindow_ReturnsFalse()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 5000, SellVolume = 5000 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.False(result);
    }

    #endregion

    #region Volume Ratio Detection

    [Fact]
    public void IsSuspicious_ExtremeVolumeRatio_ReturnsTrue()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 50000, SellVolume = 1000 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.True(result);
    }

    [Fact]
    public void IsSuspicious_NormalVolumeRatio_ReturnsFalse()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 5000, SellVolume = 4000 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.False(result);
    }

    [Fact]
    public void IsSuspicious_ZeroVolumeInOneDirection_ReturnsFalse()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 5000, SellVolume = 0 }
            }
        };

        var result = _sut.IsSuspicious(priceData, 50.0);

        Assert.False(result);
    }

    #endregion
}
