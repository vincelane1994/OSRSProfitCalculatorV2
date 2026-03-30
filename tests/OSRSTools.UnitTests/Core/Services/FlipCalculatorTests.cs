using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Core.ValueObjects;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class FlipCalculatorTests
{
    private readonly Mock<IProfitCalculationService> _mockProfitCalcService;
    private readonly FlipCalculator _sut;

    public FlipCalculatorTests()
    {
        _mockProfitCalcService = new Mock<IProfitCalculationService>();
        _sut = new FlipCalculator(_mockProfitCalcService.Object);
    }

    private static ItemPriceData CreatePriceData(int volume24Hr, int? avg5mBuy = null, int? avg5mSell = null, int? avg6hBuy = null, int? avg6hSell = null)
    {
        var windows = new Dictionary<TimeWindow, TimeWindowPrice>
        {
            [TimeWindow.TwentyFourHour] = new() { BuyVolume = volume24Hr / 2, SellVolume = volume24Hr / 2 }
        };
        if (avg5mBuy.HasValue || avg5mSell.HasValue)
            windows[TimeWindow.FiveMinute] = new() { AvgBuyPrice = avg5mBuy, AvgSellPrice = avg5mSell };
        if (avg6hBuy.HasValue || avg6hSell.HasValue)
            windows[TimeWindow.SixHour] = new() { AvgBuyPrice = avg6hBuy, AvgSellPrice = avg6hSell };
        return new ItemPriceData { ItemId = 0, TimeWindows = windows };
    }

    #region CalculateFlip - Standard Profitable Scenarios

    [Fact]
    public void CalculateFlip_StandardProfitableFlip_ReturnsCorrectValues()
    {
        // Arrange
        var itemId = 1234;
        var name = "Dragon bones";
        var members = true;
        var buyLimit = 10000;
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 2000,
            RecommendedSellPrice = 2200,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var priceData = CreatePriceData(50000);
        var settings = new FlipSettings
        {
            MaxInvestment = 10_000_000,
            BuyLimitCycleHours = 4.0
        };

        var taxResult = new TaxCalculation { TaxAmount = 40, NetAfterTax = 2160, WasCapped = false, SellPrice = 2200 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(2200)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(2000, 10_000_000, 10000)).Returns(5000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(10000, 5000, 50000, 4.0)).Returns(6.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(800000L, 6.0)).Returns(133333.33);

        // Act
        var result = _sut.CalculateFlip(itemId, name, members, buyLimit, prices, priceData, settings);

        // Assert
        Assert.Equal(1234, result.ItemId);
        Assert.Equal("Dragon bones", result.Name);
        Assert.True(result.Members);
        Assert.Equal(10000, result.BuyLimit);
        Assert.Equal(2000, result.RecommendedBuyPrice);
        Assert.Equal(2200, result.RecommendedSellPrice);
        Assert.Equal(200, result.Margin);
        Assert.Equal(40, result.TaxAmount);
        Assert.Equal(160, result.ProfitPerUnit);
        Assert.Equal(5000, result.Quantity);
        Assert.Equal(800000L, result.TotalProfit);
        Assert.Equal(8.0, result.RoiPercent);
        Assert.Equal(133333.33, result.GpPerHour);
        Assert.Equal(6.0, result.EstimatedFillHours);
        Assert.Equal(50000, result.Volume24Hr);
        Assert.True(result.HasSufficientData);
        Assert.Equal(3, result.BuyWindowsUsed);
        Assert.Equal(3, result.SellWindowsUsed);
        Assert.Equal(1_600_000L, result.ProfitPerCycle); // 160 * 10000
        Assert.Equal(0, result.ConfidenceRating);
        Assert.Equal(0, result.FlipScore);
        Assert.True(result.IsProfitable);
    }

    [Fact]
    public void CalculateFlip_HighValueFlip_UsesLongForTotalProfit()
    {
        // Arrange
        var itemId = 2500;
        var name = "Twisted bow";
        var buyLimit = 8;
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1_200_000_000,
            RecommendedSellPrice = 1_250_000_000,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(100);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 5_000_000, NetAfterTax = 1_245_000_000, WasCapped = true, SellPrice = 1_250_000_000 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1_250_000_000)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(1_200_000_000, 10_000_000, 8)).Returns(1);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(8, 1, 100, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(45_000_000L, 4.0)).Returns(11_250_000.0);

        // Act
        var result = _sut.CalculateFlip(itemId, name, false, buyLimit, prices, priceData, settings);

        // Assert
        Assert.Equal(50_000_000, result.Margin);
        Assert.Equal(5_000_000, result.TaxAmount);
        Assert.Equal(45_000_000, result.ProfitPerUnit);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(45_000_000L, result.TotalProfit);
        Assert.Equal(3.75, result.RoiPercent);
        Assert.Equal(360_000_000L, result.ProfitPerCycle); // 45M * 8
    }

    #endregion

    #region CalculateFlip - Unprofitable Scenarios

    [Fact]
    public void CalculateFlip_UnprofitableFlip_ReturnsNegativeProfit()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 5000,
            RecommendedSellPrice = 4800,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 96, NetAfterTax = 4704, WasCapped = false, SellPrice = 4800 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(4800)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(5000, 10_000_000, 1000)).Returns(2000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 2000, 10000, 4.0)).Returns(8.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-592000L, 8.0)).Returns(-74000.0);

        // Act
        var result = _sut.CalculateFlip(123, "Junk item", false, 1000, prices, priceData, settings);

        // Assert
        Assert.Equal(-200, result.Margin);
        Assert.Equal(96, result.TaxAmount);
        Assert.Equal(-296, result.ProfitPerUnit);
        Assert.Equal(-592000L, result.TotalProfit);
        Assert.Equal(-5.92, result.RoiPercent);
        Assert.False(result.IsProfitable);
    }

    [Fact]
    public void CalculateFlip_MarginNegativeAfterTax_IsUnprofitable()
    {
        // Arrange: Margin is positive but profit is negative after tax
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 10000,
            RecommendedSellPrice = 10050,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(20000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 201, NetAfterTax = 9849, WasCapped = false, SellPrice = 10050 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(10050)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(10000, 10_000_000, 5000)).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(5000, 1000, 20000, 4.0)).Returns(5.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-151000L, 5.0)).Returns(-30200.0);

        // Act
        var result = _sut.CalculateFlip(456, "Low margin item", true, 5000, prices, priceData, settings);

        // Assert
        Assert.Equal(50, result.Margin);
        Assert.Equal(201, result.TaxAmount);
        Assert.Equal(-151, result.ProfitPerUnit);
        Assert.False(result.IsProfitable);
    }

    #endregion

    #region CalculateFlip - Edge Cases

    [Fact]
    public void CalculateFlip_ZeroBuyPrice_ReturnsZeroRoiAndQuantity()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 0,
            RecommendedSellPrice = 100,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(5000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 2, NetAfterTax = 98, WasCapped = false, SellPrice = 100 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(100)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(0, 10_000_000, 1000)).Returns(0);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 0, 5000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(0L, 4.0)).Returns(0.0);

        var result = _sut.CalculateFlip(789, "Free item", false, 1000, prices, priceData, settings);

        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(0, result.Quantity);
        Assert.Equal(0.0, result.RoiPercent);
        Assert.Equal(0L, result.TotalProfit);
    }

    [Fact]
    public void CalculateFlip_ZeroSellPrice_ReturnsNegativeProfit()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 100,
            RecommendedSellPrice = 0,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(1000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 0, NetAfterTax = 0, WasCapped = false, SellPrice = 0 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(0)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(100, 10_000_000, 500)).Returns(100000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(500, 100000, 1000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-10000000L, 4.0)).Returns(-2500000.0);

        var result = _sut.CalculateFlip(999, "Worthless item", false, 500, prices, priceData, settings);

        Assert.Equal(-100, result.Margin);
        Assert.Equal(-100, result.ProfitPerUnit);
        Assert.False(result.IsProfitable);
    }

    #endregion

    #region CalculateFlip - New Fields

    [Fact]
    public void CalculateFlip_PopulatesWindowCounts_FromPriceRecommendation()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 4
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(100, "Test", false, 1000, prices, priceData, settings);

        Assert.Equal(3, result.BuyWindowsUsed);
        Assert.Equal(4, result.SellWindowsUsed);
    }

    [Fact]
    public void CalculateFlip_ComputesVolatility_From5mVs6hPrices()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        // 5m buy: 110, 6h buy: 100 → 10% dev; 5m sell: 95, 6h sell: 90 → 5.55% dev → max = 10%
        var priceData = CreatePriceData(10000, avg5mBuy: 110, avg5mSell: 95, avg6hBuy: 100, avg6hSell: 90);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(100, "Volatile item", false, 1000, prices, priceData, settings);

        Assert.Equal(10.0, result.PriceVolatilityPercent, precision: 1);
    }

    [Fact]
    public void CalculateFlip_Volatility_ZeroWhenMissing5mOr6hData()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        // Only 24h window, no 5m or 6h
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(100, "No 5m data", false, 1000, prices, priceData, settings);

        Assert.Equal(0.0, result.PriceVolatilityPercent);
    }

    [Fact]
    public void CalculateFlip_ProfitPerCycle_CalculatedCorrectly()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 500,
            RecommendedSellPrice = 600,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var priceData = CreatePriceData(50000);
        var buyLimit = 10000;
        var settings = new FlipSettings { MaxInvestment = 10_000_000 };

        var taxResult = new TaxCalculation { TaxAmount = 12 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(600)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(500, 10_000_000, 10000)).Returns(10000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(220000.0);

        var result = _sut.CalculateFlip(200, "Consumable", false, buyLimit, prices, priceData, settings);

        // ProfitPerUnit = 100 - 12 = 88, ProfitPerCycle = 88 * 10000 = 880,000
        Assert.Equal(88, result.ProfitPerUnit);
        Assert.Equal(880_000L, result.ProfitPerCycle);
    }

    #endregion

    #region CalculateFlip - Tax Delegation

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForTax_VerifiesCall()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1500,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(1000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 30, NetAfterTax = 1470, WasCapped = false, SellPrice = 1500 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1500)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(100);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(1000.0);

        var result = _sut.CalculateFlip(100, "Test", false, 100, prices, priceData, settings);

        _mockProfitCalcService.Verify(x => x.CalculateTax(1500), Times.Once);
        Assert.Equal(30, result.TaxAmount);
    }

    #endregion

    #region CalculateFlip - Quantity

    [Fact]
    public void CalculateFlip_QuantityLimitedByBuyLimit_UsesCorrectValue()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 500,
            RecommendedSellPrice = 600,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var priceData = CreatePriceData(50000);
        var settings = new FlipSettings { MaxInvestment = 10_000_000 };
        var buyLimit = 1000;

        var taxResult = new TaxCalculation { TaxAmount = 12 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(600)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(500, 10_000_000, 1000)).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 1000, 50000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(88000L, 4.0)).Returns(22000.0);

        var result = _sut.CalculateFlip(200, "Low limit", false, buyLimit, prices, priceData, settings);

        _mockProfitCalcService.Verify(x => x.CalculateMaxQuantity(500, 10_000_000, 1000), Times.Once);
        Assert.Equal(1000, result.Quantity);
    }

    #endregion

    #region CalculateFlip - HasSufficientData

    [Fact]
    public void CalculateFlip_SufficientData_PassesThrough()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(400, "Good data", false, 1000, prices, priceData, settings);

        Assert.True(result.HasSufficientData);
    }

    [Fact]
    public void CalculateFlip_InsufficientData_PassesThrough()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 1,
            WindowsUsedForSell = 1
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(500, "Bad data", false, 1000, prices, priceData, settings);

        Assert.False(result.HasSufficientData);
    }

    #endregion

    #region CalculateFlip - ConfidenceRating and FlipScore

    [Fact]
    public void CalculateFlip_AlwaysReturnsZeroConfidenceRatingAndFlipScore()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1500,
            WindowsUsedForBuy = 4,
            WindowsUsedForSell = 4
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 30 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(100000.0);

        var result = _sut.CalculateFlip(600, "Test", false, 1000, prices, priceData, settings);

        Assert.Equal(0, result.ConfidenceRating);
        Assert.Equal(0, result.FlipScore);
    }

    #endregion

    #region CalculateFlip - ROI Calculation

    [Fact]
    public void CalculateFlip_RoiPercent_RoundsToTwoDecimals()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 3000,
            RecommendedSellPrice = 3100,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 62 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(3100)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(9500.0);

        var result = _sut.CalculateFlip(900, "ROI test", false, 1000, prices, priceData, settings);

        // ProfitPerUnit = 100 - 62 = 38, ROI = (38 / 3000) * 100 = 1.2666...
        Assert.Equal(1.27, result.RoiPercent);
    }

    #endregion

    #region CalculateFlip - TotalProfit Long Type

    [Fact]
    public void CalculateFlip_TotalProfit_UsesLongToAvoidOverflow()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 100_000,
            RecommendedSellPrice = 150_000,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var priceData = CreatePriceData(50000);
        var settings = new FlipSettings { MaxInvestment = 1_000_000_000 };

        var taxResult = new TaxCalculation { TaxAmount = 3000 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(150_000)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(100_000, 1_000_000_000, 25000)).Returns(10000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(25000, 10000, 50000, 4.0)).Returns(10.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(470_000_000L, 10.0)).Returns(47_000_000.0);

        var result = _sut.CalculateFlip(1000, "Big flip", false, 25000, prices, priceData, settings);

        Assert.Equal(470_000_000L, result.TotalProfit);
        Assert.IsType<long>(result.TotalProfit);
    }

    #endregion

    #region CalculateFlip - Fill Hours and GP/hr Delegation

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForFillHours_VerifiesCall()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(30000);
        var settings = new FlipSettings { BuyLimitCycleHours = 4.0 };
        var buyLimit = 5000;

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(3000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(5000, 3000, 30000, 4.0)).Returns(5.5);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(50000.0);

        var result = _sut.CalculateFlip(1100, "Fill test", false, buyLimit, prices, priceData, settings);

        _mockProfitCalcService.Verify(x => x.CalculateEstimatedFillHours(5000, 3000, 30000, 4.0), Times.Once);
        Assert.Equal(5.5, result.EstimatedFillHours);
    }

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForGpPerHour_VerifiesCall()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 2000,
            RecommendedSellPrice = 2500,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(25000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 50 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(2500)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(2000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(6.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(900000L, 6.0)).Returns(150000.0);

        var result = _sut.CalculateFlip(1200, "GP/hr test", true, 5000, prices, priceData, settings);

        // ProfitPerUnit = 500 - 50 = 450, TotalProfit = 450 * 2000 = 900000
        _mockProfitCalcService.Verify(x => x.CalculateGpPerHour(900000L, 6.0), Times.Once);
        Assert.Equal(150000.0, result.GpPerHour);
    }

    #endregion

    #region CalculateFlip - Best Volume Across Windows

    [Fact]
    public void CalculateFlip_UsesMaxVolumeAcrossWindows_ForFillTime()
    {
        // 24h window: 10,000 volume → extrapolated = 10,000
        // 1h window: 2,000 volume → extrapolated = 2,000 * 24 = 48,000
        // Effective volume should be 48,000
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = new ItemPriceData
        {
            ItemId = 0,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.TwentyFourHour] = new() { BuyVolume = 5000, SellVolume = 5000 },
                [TimeWindow.OneHour] = new() { BuyVolume = 1000, SellVolume = 1000 }
            }
        };
        var settings = new FlipSettings { BuyLimitCycleHours = 4.0 };

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(100, "Multi-window", false, 1000, prices, priceData, settings);

        // Fill time should use effective volume 48,000 (not raw 24h volume 10,000)
        _mockProfitCalcService.Verify(x => x.CalculateEstimatedFillHours(1000, 1000, 48000, 4.0), Times.Once);
        // Display volume should still be raw 24h value
        Assert.Equal(10000, result.Volume24Hr);
    }

    [Fact]
    public void CalculateFlip_24hVolumeHighest_Uses24hVolume()
    {
        // 24h window: 100,000 volume → extrapolated = 100,000
        // 1h window: 1,000 volume → extrapolated = 1,000 * 24 = 24,000
        // Effective volume should be 100,000
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = new ItemPriceData
        {
            ItemId = 0,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.TwentyFourHour] = new() { BuyVolume = 50000, SellVolume = 50000 },
                [TimeWindow.OneHour] = new() { BuyVolume = 500, SellVolume = 500 }
            }
        };
        var settings = new FlipSettings { BuyLimitCycleHours = 4.0 };

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        var result = _sut.CalculateFlip(100, "High 24h vol", false, 1000, prices, priceData, settings);

        // When 24h has highest extrapolated volume, it should be used
        _mockProfitCalcService.Verify(x => x.CalculateEstimatedFillHours(1000, 1000, 100000, 4.0), Times.Once);
    }

    #endregion

    #region CalculateFlip - IsProfitable

    [Fact]
    public void CalculateFlip_IsProfitable_ReflectsProfitPerUnit()
    {
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1300,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var priceData = CreatePriceData(10000);
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 26 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1300)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(68500.0);

        var result = _sut.CalculateFlip(1300, "Profitable", false, 1000, prices, priceData, settings);

        Assert.Equal(274, result.ProfitPerUnit); // 300 - 26
        Assert.True(result.IsProfitable);
    }

    #endregion
}
