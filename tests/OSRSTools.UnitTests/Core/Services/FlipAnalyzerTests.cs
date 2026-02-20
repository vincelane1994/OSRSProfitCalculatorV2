using Microsoft.Extensions.Logging;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Core.ValueObjects;
using Xunit;

namespace OSRSTools.UnitTests.Core.Services;

public class FlipAnalyzerTests
{
    private readonly Mock<IDataFetchService> _mockDataFetch;
    private readonly Mock<IPriceRecommendationService> _mockPriceRec;
    private readonly Mock<IFlipCalculator> _mockFlipCalc;
    private readonly Mock<IScoringService> _mockScoring;
    private readonly Mock<IManipulationDetector> _mockManipDetector;
    private readonly FlipAnalyzer _sut;
    private readonly FlipSettings _settings;

    public FlipAnalyzerTests()
    {
        _mockDataFetch = new Mock<IDataFetchService>();
        _mockPriceRec = new Mock<IPriceRecommendationService>();
        _mockFlipCalc = new Mock<IFlipCalculator>();
        _mockScoring = new Mock<IScoringService>();
        _mockManipDetector = new Mock<IManipulationDetector>();
        var logger = new Mock<ILogger<FlipAnalyzer>>();

        _sut = new FlipAnalyzer(
            _mockDataFetch.Object, _mockPriceRec.Object, _mockFlipCalc.Object,
            _mockScoring.Object, _mockManipDetector.Object, logger.Object);

        _settings = new FlipSettings { MinVolume = 10000, MinBuyLimit = 100, MinMargin = 10 };
    }

    private void SetupMappingsAndPrices(
        Dictionary<int, ItemMapping> mappings,
        Dictionary<int, ItemPriceData> prices)
    {
        _mockDataFetch.Setup(s => s.GetMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);
        _mockDataFetch.Setup(s => s.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prices);
    }

    private static ItemPriceData CreatePriceData(int itemId, int volume24Hr)
    {
        return new ItemPriceData
        {
            ItemId = itemId,
            TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
            {
                [TimeWindow.FiveMinute] = new() { AvgBuyPrice = 110, AvgSellPrice = 95 },
                [TimeWindow.OneHour] = new() { AvgBuyPrice = 108, AvgSellPrice = 93 },
                [TimeWindow.SixHour] = new() { AvgBuyPrice = 105, AvgSellPrice = 92 },
                [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = 100, AvgSellPrice = 90, BuyVolume = volume24Hr / 2, SellVolume = volume24Hr / 2 }
            }
        };
    }

    #region Filtering

    [Fact]
    public async Task AnalyzeFlipsAsync_BelowMinVolume_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Low Vol", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 5000) });

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
        _mockFlipCalc.Verify(s => s.CalculateFlip(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<PriceRecommendation>(),
            It.IsAny<int>(), It.IsAny<FlipSettings>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_BelowMinBuyLimit_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Low Limit", BuyLimit = 50 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_InsufficientData_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Bad Data", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });
        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                WindowsUsedForBuy = 1, WindowsUsedForSell = 1,
                RecommendedBuyPrice = 100, RecommendedSellPrice = 110
            });

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_BelowMinMargin_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Low Margin", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });
        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                WindowsUsedForBuy = 4, WindowsUsedForSell = 4,
                RecommendedBuyPrice = 100, RecommendedSellPrice = 105 // margin 5 < MinMargin 10
            });

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_ManipulatedItem_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Manipulated", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });
        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = 100, RecommendedSellPrice = 200,
                WindowsUsedForBuy = 4, WindowsUsedForSell = 4
            });
        _mockManipDetector.Setup(s => s.IsSuspicious(It.IsAny<ItemPriceData>(), It.IsAny<double>()))
            .Returns(true);

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_NoPriceData_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "No Prices", BuyLimit = 500 } },
            new Dictionary<int, ItemPriceData>());

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_UnprofitableFlip_FilteredOut()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Unprofitable", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });
        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = 100, RecommendedSellPrice = 200,
                WindowsUsedForBuy = 4, WindowsUsedForSell = 4
            });
        _mockManipDetector.Setup(s => s.IsSuspicious(It.IsAny<ItemPriceData>(), It.IsAny<double>()))
            .Returns(false);
        _mockFlipCalc.Setup(s => s.CalculateFlip(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<PriceRecommendation>(),
                It.IsAny<int>(), It.IsAny<FlipSettings>()))
            .Returns(new FlipCandidate { ProfitPerUnit = -5 }); // IsProfitable = false

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Empty(result);
    }

    #endregion

    #region Successful Pipeline

    [Fact]
    public async Task AnalyzeFlipsAsync_ValidCandidate_ReturnsScored()
    {
        SetupMappingsAndPrices(
            new() { [1] = new ItemMapping { ItemId = 1, Name = "Good Item", BuyLimit = 500 } },
            new() { [1] = CreatePriceData(1, volume24Hr: 50000) });
        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = 100, RecommendedSellPrice = 200,
                WindowsUsedForBuy = 4, WindowsUsedForSell = 4
            });
        _mockManipDetector.Setup(s => s.IsSuspicious(It.IsAny<ItemPriceData>(), It.IsAny<double>()))
            .Returns(false);
        _mockFlipCalc.Setup(s => s.CalculateFlip(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<PriceRecommendation>(),
                It.IsAny<int>(), It.IsAny<FlipSettings>()))
            .Returns(new FlipCandidate { ProfitPerUnit = 50, GpPerHour = 100000 });
        _mockScoring.Setup(s => s.CalculateFlipScore(It.IsAny<FlipCandidate>())).Returns(7.5);
        _mockScoring.Setup(s => s.CalculateConfidence(It.IsAny<int>(), It.IsAny<int>())).Returns(0.9);

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Single(result);
        Assert.Equal(7.5, result[0].FlipScore);
        Assert.Equal(0.9, result[0].ConfidenceRating);
    }

    [Fact]
    public async Task AnalyzeFlipsAsync_MultipleItems_SortedByGpPerHourDescending()
    {
        var mappings = new Dictionary<int, ItemMapping>
        {
            [1] = new() { ItemId = 1, Name = "Low GP", BuyLimit = 500 },
            [2] = new() { ItemId = 2, Name = "High GP", BuyLimit = 500 }
        };
        var prices = new Dictionary<int, ItemPriceData>
        {
            [1] = CreatePriceData(1, 50000),
            [2] = CreatePriceData(2, 50000)
        };
        SetupMappingsAndPrices(mappings, prices);

        _mockPriceRec.Setup(s => s.CalculateRecommendedPrices(It.IsAny<ItemPriceData>()))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = 100, RecommendedSellPrice = 200,
                WindowsUsedForBuy = 4, WindowsUsedForSell = 4
            });
        _mockManipDetector.Setup(s => s.IsSuspicious(It.IsAny<ItemPriceData>(), It.IsAny<double>()))
            .Returns(false);
        _mockFlipCalc.Setup(s => s.CalculateFlip(1, It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<PriceRecommendation>(),
                It.IsAny<int>(), It.IsAny<FlipSettings>()))
            .Returns(new FlipCandidate { ItemId = 1, ProfitPerUnit = 50, GpPerHour = 50000 });
        _mockFlipCalc.Setup(s => s.CalculateFlip(2, It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<PriceRecommendation>(),
                It.IsAny<int>(), It.IsAny<FlipSettings>()))
            .Returns(new FlipCandidate { ItemId = 2, ProfitPerUnit = 100, GpPerHour = 200000 });
        _mockScoring.Setup(s => s.CalculateFlipScore(It.IsAny<FlipCandidate>())).Returns(5.0);
        _mockScoring.Setup(s => s.CalculateConfidence(It.IsAny<int>(), It.IsAny<int>())).Returns(0.8);

        var result = await _sut.AnalyzeFlipsAsync(_settings);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].ItemId); // Higher GP/hr first
        Assert.Equal(1, result[1].ItemId);
    }

    #endregion
}
