using Microsoft.Extensions.Logging;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.UnitTests.Core.Services;

public class HighAlchingServiceTests
{
    private readonly Mock<IDataFetchService> _dataFetchMock = new();
    private readonly Mock<IProfitCalculationService> _profitCalcMock = new();
    private readonly Mock<IPriceRecommendationService> _priceRecommendationMock = new();
    private readonly Mock<ILogger<HighAlchingService>> _loggerMock = new();
    private readonly HighAlchingService _sut;

    private const int NatureRuneId = 561;

    public HighAlchingServiceTests()
    {
        _sut = new HighAlchingService(
            _dataFetchMock.Object,
            _profitCalcMock.Object,
            _priceRecommendationMock.Object,
            _loggerMock.Object);
    }

    #region GetProfitableItemsAsync

    [Fact]
    public async Task GetProfitableItemsAsync_ProfitableItems_ReturnsCorrectResults()
    {
        // Arrange — nature rune only in prices, not in mappings (won't be iterated)
        var mappings = CreateMappings(
            (1, "Rune Platebody", 70, true));

        var prices = CreatePrices(
            (1, 38000, 500),
            (NatureRuneId, 120, 50000));

        SetupMocks(mappings, prices, highAlchValues: new Dictionary<int, int?> { [1] = 39000 });
        SetupNatureRunePrice(prices, buyPrice: 120);
        SetupRecommendedPrice(1, buyPrice: 38000);

        // totalCost = 38000 + 120 = 38120, revenue = 39000
        _profitCalcMock
            .Setup(x => x.CalculateSimpleProfit(38120, 39000, 70))
            .Returns(new ProfitCalculation
            {
                ProfitPerUnit = 880,
                CostPerUnit = 38120,
                RevenuePerUnit = 39000,
                Quantity = 70,
                TotalInvestment = 2_668_400,
                TotalProfit = 61_600,
                RoiPercent = 2.31
            });

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Rune Platebody", result[0].Name);
        Assert.Equal(880, result[0].Profit);
        Assert.Equal(38000, result[0].BuyPrice);
        Assert.Equal(39000, result[0].HighAlchValue);
        Assert.Equal(120, result[0].NatureRuneCost);
        Assert.True(result[0].IsProfitable);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_ZeroVolumeItems_AreExcluded()
    {
        // Arrange
        var mappings = CreateMappings(
            (1, "Low Volume Item", 100, false));

        var prices = CreatePrices(
            (1, 100, 0),  // Zero volume
            (NatureRuneId, 120, 50000));

        SetupMocks(mappings, prices, highAlchValues: new Dictionary<int, int?> { [1] = 200 });
        SetupNatureRunePrice(prices, buyPrice: 120);
        SetupRecommendedPrice(1, buyPrice: 100);

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_ItemsWithNoHighAlchValue_AreExcluded()
    {
        // Arrange
        var mappings = CreateMappings(
            (1, "No Alch Item", 100, false));

        var prices = CreatePrices(
            (1, 100, 500),
            (NatureRuneId, 120, 50000));

        SetupMocks(mappings, prices, highAlchValues: new Dictionary<int, int?> { [1] = null });
        SetupNatureRunePrice(prices, buyPrice: 120);

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_NatureRuneCostDeducted_FromProfit()
    {
        // Arrange
        var mappings = CreateMappings(
            (1, "Test Item", 100, false));

        var prices = CreatePrices(
            (1, 500, 1000),
            (NatureRuneId, 200, 50000));

        SetupMocks(mappings, prices, highAlchValues: new Dictionary<int, int?> { [1] = 900 });
        SetupNatureRunePrice(prices, buyPrice: 200);
        SetupRecommendedPrice(1, buyPrice: 500);

        // totalCost = 500 + 200 = 700, profit = 900 - 700 = 200
        _profitCalcMock
            .Setup(x => x.CalculateSimpleProfit(700, 900, 100))
            .Returns(new ProfitCalculation { RoiPercent = 28.57 });

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(200, result[0].Profit); // 900 - 500 - 200
        Assert.Equal(200, result[0].NatureRuneCost);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_EmptyData_ReturnsEmptyList()
    {
        // Arrange
        var mappings = new Dictionary<int, ItemMapping>();
        var prices = new Dictionary<int, ItemPriceData>();

        _dataFetchMock.Setup(x => x.GetMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemMapping>)mappings);
        _dataFetchMock.Setup(x => x.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemPriceData>)prices);

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_MembersAndF2P_BothIncluded()
    {
        // Arrange
        var mappings = CreateMappings(
            (1, "Members Item", 100, true),
            (2, "F2P Item", 100, false));

        var prices = CreatePrices(
            (1, 500, 1000),
            (2, 300, 2000),
            (NatureRuneId, 120, 50000));

        SetupMocks(mappings, prices, highAlchValues: new Dictionary<int, int?>
        {
            [1] = 800,
            [2] = 600
        });
        SetupNatureRunePrice(prices, buyPrice: 120);
        SetupRecommendedPrice(1, buyPrice: 500);
        SetupRecommendedPrice(2, buyPrice: 300);

        _profitCalcMock
            .Setup(x => x.CalculateSimpleProfit(620, 800, 100))
            .Returns(new ProfitCalculation { RoiPercent = 29.03 });
        _profitCalcMock
            .Setup(x => x.CalculateSimpleProfit(420, 600, 100))
            .Returns(new ProfitCalculation { RoiPercent = 42.86 });

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Members);
        Assert.Contains(result, x => !x.Members);
    }

    [Fact]
    public async Task GetProfitableItemsAsync_NoNatureRunePrice_ReturnsEmpty()
    {
        // Arrange — no nature rune in prices at all
        var mappings = CreateMappings(
            (1, "Test Item", 100, false));

        var prices = CreatePrices(
            (1, 500, 1000));

        _dataFetchMock.Setup(x => x.GetMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemMapping>)mappings);
        _dataFetchMock.Setup(x => x.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemPriceData>)prices);

        // Act
        var result = await _sut.GetProfitableItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Test Helpers

    private static Dictionary<int, ItemMapping> CreateMappings(params (int id, string name, int buyLimit, bool members)[] items)
    {
        return items.ToDictionary(
            x => x.id,
            x => new ItemMapping
            {
                ItemId = x.id,
                Name = x.name,
                BuyLimit = x.buyLimit,
                Members = x.members
            });
    }

    private static Dictionary<int, ItemPriceData> CreatePrices(params (int id, int buyPrice, int volume)[] items)
    {
        return items.ToDictionary(
            x => x.id,
            x => new ItemPriceData
            {
                ItemId = x.id,
                LatestBuyPrice = x.buyPrice,
                TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
                {
                    [TimeWindow.OneHour] = new() { AvgBuyPrice = x.buyPrice, AvgSellPrice = x.buyPrice - 5, BuyVolume = x.volume / 2, SellVolume = x.volume / 2 },
                    [TimeWindow.SixHour] = new() { AvgBuyPrice = x.buyPrice, AvgSellPrice = x.buyPrice - 5, BuyVolume = x.volume / 2, SellVolume = x.volume / 2 },
                    [TimeWindow.TwentyFourHour] = new() { AvgBuyPrice = x.buyPrice, AvgSellPrice = x.buyPrice - 5, BuyVolume = x.volume / 2, SellVolume = x.volume / 2 }
                }
            });
    }

    private void SetupMocks(
        Dictionary<int, ItemMapping> mappings,
        Dictionary<int, ItemPriceData> prices,
        Dictionary<int, int?> highAlchValues)
    {
        _dataFetchMock.Setup(x => x.GetMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemMapping>)mappings);
        _dataFetchMock.Setup(x => x.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemPriceData>)prices);

        foreach (var (itemId, alchValue) in highAlchValues)
        {
            _dataFetchMock.Setup(x => x.GetHighAlchValueAsync(itemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alchValue);
        }
    }

    private void SetupNatureRunePrice(Dictionary<int, ItemPriceData> prices, int buyPrice)
    {
        _priceRecommendationMock
            .Setup(x => x.CalculateRecommendedPrices(It.Is<ItemPriceData>(p => p.ItemId == NatureRuneId)))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = buyPrice,
                RecommendedSellPrice = buyPrice + 5,
                WindowsUsedForBuy = 2,
                WindowsUsedForSell = 2
            });
    }

    private void SetupRecommendedPrice(int itemId, int buyPrice)
    {
        _priceRecommendationMock
            .Setup(x => x.CalculateRecommendedPrices(It.Is<ItemPriceData>(p => p.ItemId == itemId)))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice = buyPrice,
                RecommendedSellPrice = buyPrice + 10,
                WindowsUsedForBuy = 2,
                WindowsUsedForSell = 2
            });
    }

    #endregion
}
