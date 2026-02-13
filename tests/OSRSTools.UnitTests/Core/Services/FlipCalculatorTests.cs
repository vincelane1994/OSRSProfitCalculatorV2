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
        var volume24Hr = 50000;
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
        var result = _sut.CalculateFlip(itemId, name, members, buyLimit, prices, volume24Hr, settings);

        // Assert
        Assert.Equal(1234, result.ItemId);
        Assert.Equal("Dragon bones", result.Name);
        Assert.True(result.Members);
        Assert.Equal(10000, result.BuyLimit);
        Assert.Equal(2000, result.RecommendedBuyPrice);
        Assert.Equal(2200, result.RecommendedSellPrice);
        Assert.Equal(200, result.Margin); // 2200 - 2000
        Assert.Equal(40, result.TaxAmount);
        Assert.Equal(160, result.ProfitPerUnit); // 200 - 40
        Assert.Equal(5000, result.Quantity);
        Assert.Equal(800000L, result.TotalProfit); // 160 * 5000
        Assert.Equal(8.0, result.RoiPercent); // (160 / 2000) * 100 = 8.0
        Assert.Equal(133333.33, result.GpPerHour);
        Assert.Equal(6.0, result.EstimatedFillHours);
        Assert.Equal(50000, result.Volume24Hr);
        Assert.True(result.HasSufficientData); // 3 windows >= 2
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
        var volume24Hr = 100;
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 5_000_000, NetAfterTax = 1_245_000_000, WasCapped = true, SellPrice = 1_250_000_000 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1_250_000_000)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(1_200_000_000, 10_000_000, 8)).Returns(1);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(8, 1, 100, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(45_000_000L, 4.0)).Returns(11_250_000.0);

        // Act
        var result = _sut.CalculateFlip(itemId, name, false, buyLimit, prices, volume24Hr, settings);

        // Assert
        Assert.Equal(50_000_000, result.Margin); // 1.25B - 1.2B
        Assert.Equal(5_000_000, result.TaxAmount);
        Assert.Equal(45_000_000, result.ProfitPerUnit); // 50M - 5M
        Assert.Equal(1, result.Quantity);
        Assert.Equal(45_000_000L, result.TotalProfit); // Uses long type
        Assert.Equal(3.75, result.RoiPercent); // (45M / 1200M) * 100 = 3.75
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
            RecommendedSellPrice = 4800, // Loss scenario
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 96, NetAfterTax = 4704, WasCapped = false, SellPrice = 4800 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(4800)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(5000, 10_000_000, 1000)).Returns(2000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 2000, 10000, 4.0)).Returns(8.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-592000L, 8.0)).Returns(-74000.0);

        // Act
        var result = _sut.CalculateFlip(123, "Junk item", false, 1000, prices, 10000, settings);

        // Assert
        Assert.Equal(-200, result.Margin); // 4800 - 5000
        Assert.Equal(96, result.TaxAmount);
        Assert.Equal(-296, result.ProfitPerUnit); // -200 - 96
        Assert.Equal(-592000L, result.TotalProfit); // -296 * 2000
        Assert.Equal(-5.92, result.RoiPercent); // (-296 / 5000) * 100
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
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 201, NetAfterTax = 9849, WasCapped = false, SellPrice = 10050 }; // High tax
        _mockProfitCalcService.Setup(x => x.CalculateTax(10050)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(10000, 10_000_000, 5000)).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(5000, 1000, 20000, 4.0)).Returns(5.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-151000L, 5.0)).Returns(-30200.0);

        // Act
        var result = _sut.CalculateFlip(456, "Low margin item", true, 5000, prices, 20000, settings);

        // Assert
        Assert.Equal(50, result.Margin); // Positive margin
        Assert.Equal(201, result.TaxAmount);
        Assert.Equal(-151, result.ProfitPerUnit); // Negative after tax
        Assert.False(result.IsProfitable);
    }

    #endregion

    #region CalculateFlip - Edge Cases

    [Fact]
    public void CalculateFlip_ZeroBuyPrice_ReturnsZeroRoiAndQuantity()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 0,
            RecommendedSellPrice = 100,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 2, NetAfterTax = 98, WasCapped = false, SellPrice = 100 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(100)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(0, 10_000_000, 1000)).Returns(0);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 0, 5000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(0L, 4.0)).Returns(0.0);

        // Act
        var result = _sut.CalculateFlip(789, "Free item", false, 1000, prices, 5000, settings);

        // Assert
        Assert.Equal(0, result.RecommendedBuyPrice);
        Assert.Equal(0, result.Quantity);
        Assert.Equal(0.0, result.RoiPercent); // Division by zero protection
        Assert.Equal(0L, result.TotalProfit);
    }

    [Fact]
    public void CalculateFlip_ZeroSellPrice_ReturnsNegativeProfit()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 100,
            RecommendedSellPrice = 0,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 0, NetAfterTax = 0, WasCapped = false, SellPrice = 0 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(0)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(100, 10_000_000, 500)).Returns(100000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(500, 100000, 1000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(-10000000L, 4.0)).Returns(-2500000.0);

        // Act
        var result = _sut.CalculateFlip(999, "Worthless item", false, 500, prices, 1000, settings);

        // Assert
        Assert.Equal(-100, result.Margin);
        Assert.Equal(-100, result.ProfitPerUnit);
        Assert.False(result.IsProfitable);
    }

    #endregion

    #region CalculateFlip - Tax Calculation Delegation

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForTax_VerifiesCall()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1500,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 30, NetAfterTax = 1470, WasCapped = false, SellPrice = 1500 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1500)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(100);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(1000.0);

        // Act
        var result = _sut.CalculateFlip(100, "Test", false, 100, prices, 1000, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateTax(1500), Times.Once);
        Assert.Equal(30, result.TaxAmount);
    }

    #endregion

    #region CalculateFlip - Quantity Calculation

    [Fact]
    public void CalculateFlip_QuantityLimitedByBuyLimit_UsesCorrectValue()
    {
        // Arrange: max investment allows 10000 but buy limit is 1000
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 500,
            RecommendedSellPrice = 600,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var settings = new FlipSettings { MaxInvestment = 10_000_000 };
        var buyLimit = 1000;

        var taxResult = new TaxCalculation { TaxAmount = 12, NetAfterTax = 588, WasCapped = false, SellPrice = 600 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(600)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(500, 10_000_000, 1000)).Returns(1000); // Limited by buy limit
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(1000, 1000, 50000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(88000L, 4.0)).Returns(22000.0);

        // Act
        var result = _sut.CalculateFlip(200, "Low limit", false, buyLimit, prices, 50000, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateMaxQuantity(500, 10_000_000, 1000), Times.Once);
        Assert.Equal(1000, result.Quantity);
    }

    [Fact]
    public void CalculateFlip_QuantityLimitedByMaxInvestment_UsesCorrectValue()
    {
        // Arrange: buy limit is 10000 but max investment only allows 500
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 10000,
            RecommendedSellPrice = 11000,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings { MaxInvestment = 5_000_000 };
        var buyLimit = 10000;

        var taxResult = new TaxCalculation { TaxAmount = 220, NetAfterTax = 10780, WasCapped = false, SellPrice = 11000 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(11000)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(10000, 5_000_000, 10000)).Returns(500); // Limited by investment
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(10000, 500, 10000, 4.0)).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(390000L, 4.0)).Returns(97500.0);

        // Act
        var result = _sut.CalculateFlip(300, "Expensive", true, buyLimit, prices, 10000, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateMaxQuantity(10000, 5_000_000, 10000), Times.Once);
        Assert.Equal(500, result.Quantity);
    }

    #endregion

    #region CalculateFlip - HasSufficientData

    [Fact]
    public void CalculateFlip_SufficientData_PassesThrough()
    {
        // Arrange: 3 windows for both buy and sell = sufficient
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24, NetAfterTax = 1176, WasCapped = false, SellPrice = 1200 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        // Act
        var result = _sut.CalculateFlip(400, "Good data", false, 1000, prices, 10000, settings);

        // Assert
        Assert.True(result.HasSufficientData);
    }

    [Fact]
    public void CalculateFlip_InsufficientData_PassesThrough()
    {
        // Arrange: Only 1 window = insufficient
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 1,
            WindowsUsedForSell = 1
        };
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24, NetAfterTax = 1176, WasCapped = false, SellPrice = 1200 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(10000.0);

        // Act
        var result = _sut.CalculateFlip(500, "Bad data", false, 1000, prices, 10000, settings);

        // Assert
        Assert.False(result.HasSufficientData);
    }

    #endregion

    #region CalculateFlip - ConfidenceRating and FlipScore

    [Fact]
    public void CalculateFlip_AlwaysReturnsZeroConfidenceRatingAndFlipScore()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1500,
            WindowsUsedForBuy = 4,
            WindowsUsedForSell = 4
        };
        var settings = new FlipSettings();

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 30, NetAfterTax = 1470, WasCapped = false, SellPrice = 1500 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(100000.0);

        // Act
        var result = _sut.CalculateFlip(600, "Test", false, 1000, prices, 10000, settings);

        // Assert - Deferred to ScoringService
        Assert.Equal(0, result.ConfidenceRating);
        Assert.Equal(0, result.FlipScore);
    }

    #endregion

    #region CalculateFlip - Custom FlipSettings

    [Fact]
    public void CalculateFlip_CustomMaxInvestment_UsesCustomValue()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 5000,
            RecommendedSellPrice = 5500,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings { MaxInvestment = 50_000_000 }; // Custom higher investment

        var taxResult = new TaxCalculation { TaxAmount = 110, NetAfterTax = 5390, WasCapped = false, SellPrice = 5500 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(5500)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(5000, 50_000_000, 10000)).Returns(10000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(10000, 10000, 100000, 4.0)).Returns(8.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(3900000L, 8.0)).Returns(487500.0);

        // Act
        var result = _sut.CalculateFlip(700, "High investment", true, 10000, prices, 100000, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateMaxQuantity(5000, 50_000_000, 10000), Times.Once);
    }

    [Fact]
    public void CalculateFlip_CustomBuyLimitCycleHours_UsesCustomValue()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 2000,
            RecommendedSellPrice = 2400,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings { BuyLimitCycleHours = 6.0 }; // Custom 6-hour cycle

        var taxResult = new TaxCalculation { TaxAmount = 48, NetAfterTax = 2352, WasCapped = false, SellPrice = 2400 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(2400)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(2000, 10_000_000, 5000)).Returns(5000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(5000, 5000, 20000, 6.0)).Returns(12.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(1760000L, 12.0)).Returns(146666.67);

        // Act
        var result = _sut.CalculateFlip(800, "Slow cycle", false, 5000, prices, 20000, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateEstimatedFillHours(5000, 5000, 20000, 6.0), Times.Once);
    }

    #endregion

    #region CalculateFlip - ROI Calculation

    [Fact]
    public void CalculateFlip_RoiPercent_RoundsToTwoDecimals()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 3000,
            RecommendedSellPrice = 3100,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 62, NetAfterTax = 3038, WasCapped = false, SellPrice = 3100 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(3100)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(9500.0);

        // Act
        var result = _sut.CalculateFlip(900, "ROI test", false, 1000, prices, 10000, settings);

        // Assert
        // ProfitPerUnit = 100 - 62 = 38
        // ROI = (38 / 3000) * 100 = 1.2666...
        Assert.Equal(1.27, result.RoiPercent); // Rounded to 2 decimals
    }

    #endregion

    #region CalculateFlip - TotalProfit Long Type

    [Fact]
    public void CalculateFlip_TotalProfit_UsesLongToAvoidOverflow()
    {
        // Arrange: Large profit per unit * large quantity
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 100_000,
            RecommendedSellPrice = 150_000,
            WindowsUsedForBuy = 3,
            WindowsUsedForSell = 3
        };
        var settings = new FlipSettings { MaxInvestment = 1_000_000_000 };

        var taxResult = new TaxCalculation { TaxAmount = 3000, NetAfterTax = 147000, WasCapped = false, SellPrice = 150_000 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(150_000)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(100_000, 1_000_000_000, 25000)).Returns(10000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(25000, 10000, 50000, 4.0)).Returns(10.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(470_000_000L, 10.0)).Returns(47_000_000.0);

        // Act
        var result = _sut.CalculateFlip(1000, "Big flip", false, 25000, prices, 50000, settings);

        // Assert
        // ProfitPerUnit = 50000 - 3000 = 47000
        // TotalProfit = 47000 * 10000 = 470,000,000 (would overflow int)
        Assert.Equal(470_000_000L, result.TotalProfit);
        Assert.IsType<long>(result.TotalProfit);
    }

    #endregion

    #region CalculateFlip - Fill Hours and GP/hr Delegation

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForFillHours_VerifiesCall()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1200,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings { BuyLimitCycleHours = 4.0 };
        var buyLimit = 5000;
        var volume24Hr = 30000;

        _mockProfitCalcService.Setup(x => x.CalculateTax(It.IsAny<int>())).Returns(new TaxCalculation { TaxAmount = 24, NetAfterTax = 1176, WasCapped = false, SellPrice = 1200 });
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(3000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(5000, 3000, 30000, 4.0)).Returns(5.5);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(50000.0);

        // Act
        var result = _sut.CalculateFlip(1100, "Fill test", false, buyLimit, prices, volume24Hr, settings);

        // Assert
        _mockProfitCalcService.Verify(x => x.CalculateEstimatedFillHours(5000, 3000, 30000, 4.0), Times.Once);
        Assert.Equal(5.5, result.EstimatedFillHours);
    }

    [Fact]
    public void CalculateFlip_DelegatesToProfitCalcServiceForGpPerHour_VerifiesCall()
    {
        // Arrange
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 2000,
            RecommendedSellPrice = 2500,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 50, NetAfterTax = 2450, WasCapped = false, SellPrice = 2500 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(2500)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(2000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(6.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(900000L, 6.0)).Returns(150000.0);

        // Act
        var result = _sut.CalculateFlip(1200, "GP/hr test", true, 5000, prices, 25000, settings);

        // Assert
        // ProfitPerUnit = 500 - 50 = 450
        // TotalProfit = 450 * 2000 = 900000
        _mockProfitCalcService.Verify(x => x.CalculateGpPerHour(900000L, 6.0), Times.Once);
        Assert.Equal(150000.0, result.GpPerHour);
    }

    #endregion

    #region CalculateFlip - IsProfitable Computed Property

    [Fact]
    public void CalculateFlip_IsProfitable_ReflectsProfitPerUnit()
    {
        // Arrange: Positive profit
        var prices = new PriceRecommendation
        {
            RecommendedBuyPrice = 1000,
            RecommendedSellPrice = 1300,
            WindowsUsedForBuy = 2,
            WindowsUsedForSell = 2
        };
        var settings = new FlipSettings();

        var taxResult = new TaxCalculation { TaxAmount = 26, NetAfterTax = 1274, WasCapped = false, SellPrice = 1300 };
        _mockProfitCalcService.Setup(x => x.CalculateTax(1300)).Returns(taxResult);
        _mockProfitCalcService.Setup(x => x.CalculateMaxQuantity(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>())).Returns(1000);
        _mockProfitCalcService.Setup(x => x.CalculateEstimatedFillHours(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>())).Returns(4.0);
        _mockProfitCalcService.Setup(x => x.CalculateGpPerHour(It.IsAny<long>(), It.IsAny<double>())).Returns(68500.0);

        // Act
        var result = _sut.CalculateFlip(1300, "Profitable", false, 1000, prices, 10000, settings);

        // Assert
        Assert.Equal(274, result.ProfitPerUnit); // 300 - 26
        Assert.True(result.IsProfitable);
    }

    #endregion
}
