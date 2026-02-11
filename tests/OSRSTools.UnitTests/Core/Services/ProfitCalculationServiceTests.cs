using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Services;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class ProfitCalculationServiceTests
{
    private readonly ProfitCalculationService _sut;

    public ProfitCalculationServiceTests()
    {
        var taxSettings = Options.Create(new TaxSettings { Rate = 0.02, Cap = 5_000_000 });
        var priceWeights = Options.Create(new PriceWeightSettings
        {
            FiveMinute = 0.10,
            OneHour = 0.35,
            SixHour = 0.35,
            TwentyFourHour = 0.20
        });
        _sut = new ProfitCalculationService(taxSettings, priceWeights);
    }

    #region CalculateTax

    [Fact]
    public void CalculateTax_StandardPrice_ReturnsCorrectTax()
    {
        var result = _sut.CalculateTax(10000);

        Assert.Equal(200, result.TaxAmount); // floor(10000 * 0.02)
        Assert.Equal(9800, result.NetAfterTax);
        Assert.False(result.WasCapped);
    }

    [Fact]
    public void CalculateTax_HighPrice_CapsCorrectly()
    {
        var result = _sut.CalculateTax(500_000_000);

        Assert.Equal(5_000_000, result.TaxAmount);
        Assert.True(result.WasCapped);
    }

    #endregion

    #region CalculateSimpleProfit

    [Fact]
    public void CalculateSimpleProfit_ProfitableItem_ReturnsCorrectValues()
    {
        var result = _sut.CalculateSimpleProfit(buyPrice: 100, sellPrice: 150, maxQuantity: 10);

        Assert.Equal(50, result.ProfitPerUnit);
        Assert.Equal(100, result.CostPerUnit);
        Assert.Equal(150, result.RevenuePerUnit);
        Assert.Equal(10, result.Quantity);
        Assert.Equal(1000, result.TotalInvestment);
        Assert.Equal(500, result.TotalProfit);
        Assert.Equal(50.0, result.RoiPercent);
        Assert.True(result.IsProfitable);
    }

    [Fact]
    public void CalculateSimpleProfit_LossItem_ReturnsNegativeProfit()
    {
        var result = _sut.CalculateSimpleProfit(buyPrice: 200, sellPrice: 100, maxQuantity: 5);

        Assert.Equal(-100, result.ProfitPerUnit);
        Assert.Equal(-500, result.TotalProfit);
        Assert.False(result.IsProfitable);
    }

    [Fact]
    public void CalculateSimpleProfit_ZeroQuantity_ReturnsDefault()
    {
        var result = _sut.CalculateSimpleProfit(buyPrice: 100, sellPrice: 150, maxQuantity: 0);

        Assert.Equal(0, result.ProfitPerUnit);
    }

    [Fact]
    public void CalculateSimpleProfit_ZeroBuyPrice_ReturnsDefault()
    {
        var result = _sut.CalculateSimpleProfit(buyPrice: 0, sellPrice: 150, maxQuantity: 10);

        Assert.Equal(0, result.ProfitPerUnit);
    }

    #endregion

    #region CalculateMultiOutputProfit

    [Fact]
    public void CalculateMultiOutputProfit_Cannonballs_ReturnsCorrectValues()
    {
        // 1 steel bar (500gp) → 4 cannonballs (200gp each = 800gp revenue)
        var result = _sut.CalculateMultiOutputProfit(
            inputPrice: 500, outputPrice: 200, outputPerInput: 4, maxQuantity: 100);

        Assert.Equal(300, result.ProfitPerUnit);  // 800 - 500
        Assert.Equal(500, result.CostPerUnit);
        Assert.Equal(800, result.RevenuePerUnit);  // 200 * 4
        Assert.Equal(100, result.Quantity);
        Assert.Equal(50000, result.TotalInvestment);
        Assert.Equal(30000, result.TotalProfit);
        Assert.Equal(60.0, result.RoiPercent);
    }

    [Fact]
    public void CalculateMultiOutputProfit_DartTips_ReturnsCorrectValues()
    {
        // 1 bar (300gp) → 10 dart tips (50gp each = 500gp revenue)
        var result = _sut.CalculateMultiOutputProfit(
            inputPrice: 300, outputPrice: 50, outputPerInput: 10, maxQuantity: 50);

        Assert.Equal(200, result.ProfitPerUnit);  // 500 - 300
        Assert.Equal(300, result.CostPerUnit);
        Assert.Equal(500, result.RevenuePerUnit);  // 50 * 10
    }

    [Fact]
    public void CalculateMultiOutputProfit_ZeroOutputPerInput_ReturnsDefault()
    {
        var result = _sut.CalculateMultiOutputProfit(
            inputPrice: 500, outputPrice: 200, outputPerInput: 0, maxQuantity: 100);

        Assert.Equal(0, result.ProfitPerUnit);
    }

    #endregion

    #region CalculateMaxQuantity

    [Fact]
    public void CalculateMaxQuantity_CapitalLimited_ReturnsCapitalQuantity()
    {
        // 1M investment, 100gp items, buy limit 50000
        var result = _sut.CalculateMaxQuantity(buyPrice: 100, maxInvestment: 1_000_000, buyLimit: 50000);

        Assert.Equal(10000, result); // 1M / 100 = 10000, less than 50000 limit
    }

    [Fact]
    public void CalculateMaxQuantity_LimitLimited_ReturnsBuyLimit()
    {
        // 100M investment, 100gp items, buy limit 5000
        var result = _sut.CalculateMaxQuantity(buyPrice: 100, maxInvestment: 100_000_000, buyLimit: 5000);

        Assert.Equal(5000, result); // 100M / 100 = 1M, capped at 5000 limit
    }

    [Fact]
    public void CalculateMaxQuantity_ZeroBuyPrice_ReturnsZero()
    {
        var result = _sut.CalculateMaxQuantity(buyPrice: 0, maxInvestment: 1_000_000, buyLimit: 5000);

        Assert.Equal(0, result);
    }

    #endregion

    #region CalculateEstimatedFillHours

    [Fact]
    public void CalculateEstimatedFillHours_NormalVolume_ReturnsCorrectHours()
    {
        // 24000 volume/24hr = 1000/hr, buy limit 5000, quantity 3000, cycle 4h
        var result = _sut.CalculateEstimatedFillHours(
            buyLimit: 5000, quantity: 3000, volume24Hr: 24000, buyLimitCycleHours: 4.0);

        // buyHours = min(5000/1000, 4) = min(5, 4) = 4
        // sellHours = min(3000/1000, 4) = min(3, 4) = 3
        Assert.Equal(7.0, result);
    }

    [Fact]
    public void CalculateEstimatedFillHours_ZeroVolume_FloorsAtOne()
    {
        var result = _sut.CalculateEstimatedFillHours(
            buyLimit: 100, quantity: 50, volume24Hr: 0, buyLimitCycleHours: 4.0);

        // hourlyVolume = max(0/24, 1) = 1
        // buyHours = min(100/1, 4) = 4
        // sellHours = min(50/1, 4) = 4
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CalculateEstimatedFillHours_HighVolume_CapsAtCycleHours()
    {
        var result = _sut.CalculateEstimatedFillHours(
            buyLimit: 10000, quantity: 10000, volume24Hr: 240, buyLimitCycleHours: 4.0);

        // hourlyVolume = max(240/24, 1) = 10
        // buyHours = min(10000/10, 4) = min(1000, 4) = 4
        // sellHours = min(10000/10, 4) = min(1000, 4) = 4
        Assert.Equal(8.0, result);
    }

    #endregion

    #region CalculateGpPerHour

    [Fact]
    public void CalculateGpPerHour_NormalValues_ReturnsCorrectGpPerHour()
    {
        var result = _sut.CalculateGpPerHour(totalProfit: 100000, estimatedFillHours: 4.0);

        Assert.Equal(25000, result);
    }

    [Fact]
    public void CalculateGpPerHour_ZeroHours_ReturnsZero()
    {
        var result = _sut.CalculateGpPerHour(totalProfit: 100000, estimatedFillHours: 0);

        Assert.Equal(0, result);
    }

    #endregion

    #region CalculateRecommendedPrices

    [Fact]
    public void CalculateRecommendedPrices_AllWindowsAvailable_ReturnsWeightedAverage()
    {
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

        var result = _sut.CalculateRecommendedPrices(priceData);

        Assert.Equal(4, result.WindowsUsedForBuy);
        Assert.Equal(4, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);
        Assert.True(result.RecommendedSellPrice > 0);
        Assert.True(result.RecommendedBuyPrice > 0);
        Assert.True(result.GrossMargin > 0);
    }

    [Fact]
    public void CalculateRecommendedPrices_MissingWindows_RedistributesWeight()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 }
            }
        };

        var result = _sut.CalculateRecommendedPrices(priceData);

        Assert.Equal(2, result.WindowsUsedForBuy);
        Assert.Equal(2, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);
    }

    [Fact]
    public void CalculateRecommendedPrices_OnlyOneWindow_InsufficientData()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90 }
            }
        };

        var result = _sut.CalculateRecommendedPrices(priceData);

        Assert.Equal(1, result.WindowsUsedForBuy);
        Assert.False(result.HasSufficientData);
    }

    [Fact]
    public void CalculateRecommendedPrices_NoWindows_ReturnsZero()
    {
        var priceData = new ItemPriceData
        {
            ItemId = 1,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>()
        };

        var result = _sut.CalculateRecommendedPrices(priceData);

        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(0, result.RecommendedSellPrice);
        Assert.Equal(0, result.WindowsUsedForBuy);
    }

    [Fact]
    public void CalculateRecommendedPrices_NullPricesInWindows_ExcludesThoseWindows()
    {
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

        var result = _sut.CalculateRecommendedPrices(priceData);

        Assert.Equal(3, result.WindowsUsedForBuy);
        Assert.Equal(3, result.WindowsUsedForSell);
        Assert.True(result.HasSufficientData);
    }

    #endregion
}
