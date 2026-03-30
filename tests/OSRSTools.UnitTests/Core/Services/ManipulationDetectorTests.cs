using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Services;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class ManipulationDetectorTests
{
    private readonly ManipulationDetector _sut;

    public ManipulationDetectorTests()
    {
        var settings = Options.Create(new ManipulationSettings
        {
            PriceDeviationThresholdPercent = 25.0,
            VolumeRatioThreshold = 10.0,
            HighRoiThresholdPercent = 8.0,
            LowVolumeThreshold = 5_000
        });
        _sut = new ManipulationDetector(settings);
    }

    #region Price Deviation Detection (threshold now 25%)

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

        Assert.False(_sut.IsSuspicious(priceData));
    }

    [Fact]
    public void IsSuspicious_HighDeviation_BuyPrice_ReturnsTrue()
    {
        // 30% deviation > 25% threshold
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 130, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        Assert.True(_sut.IsSuspicious(priceData));
    }

    [Fact]
    public void IsSuspicious_HighDeviation_SellPrice_ReturnsTrue()
    {
        // sell: |135 - 90| / 90 = 50% > 25%
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 135 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        Assert.True(_sut.IsSuspicious(priceData));
    }

    [Fact]
    public void IsSuspicious_AtExactThreshold_ReturnsFalse()
    {
        // 25% deviation exactly AT threshold should not be flagged (> not >=)
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 125, AvgSellPrice = 90 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        Assert.False(_sut.IsSuspicious(priceData));
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

        Assert.False(_sut.IsSuspicious(priceData));
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

        Assert.True(_sut.IsSuspicious(priceData));
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

        Assert.False(_sut.IsSuspicious(priceData));
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

        Assert.False(_sut.IsSuspicious(priceData));
    }

    #endregion

    #region High ROI + Low Volume Detection (new)

    [Fact]
    public void IsSuspicious_HighRoiLowVolume_ReturnsTrue()
    {
        // ROI 10% > 8% threshold, volume 2000 < 5000 threshold
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 1000, SellVolume = 1000 }
            }
        };

        Assert.True(_sut.IsSuspicious(priceData, roiPercent: 10.0));
    }

    [Fact]
    public void IsSuspicious_HighRoiHighVolume_ReturnsFalse()
    {
        // ROI 10% > 8% threshold, but volume 50000 > 5000 threshold → not suspicious
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 25000, SellVolume = 25000 }
            }
        };

        Assert.False(_sut.IsSuspicious(priceData, roiPercent: 10.0));
    }

    [Fact]
    public void IsSuspicious_LowRoiLowVolume_ReturnsFalse()
    {
        // ROI 3% < 8% threshold → not suspicious even with low volume
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 105, AvgSellPrice = 95 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = 1000, SellVolume = 1000 }
            }
        };

        Assert.False(_sut.IsSuspicious(priceData, roiPercent: 3.0));
    }

    #endregion
}
