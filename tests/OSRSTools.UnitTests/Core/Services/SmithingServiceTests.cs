using Microsoft.Extensions.Logging;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.UnitTests.Core.Services;

public class SmithingServiceTests
{
    private readonly Mock<IDataFetchService> _dataFetchMock = new();
    private readonly Mock<IProfitCalculationService> _profitCalcMock = new();
    private readonly Mock<IPriceRecommendationService> _priceRecommendationMock = new();
    private readonly Mock<ILogger<SmithingService>> _loggerMock = new();
    private readonly SmithingService _sut;

    // Cannonball recipe constants
    private const int SteelBarId    = 2353;
    private const int CannonballId  = 2;

    // Dart tip bar IDs
    private const int BronzeBarId  = 2349;
    private const int IronBarId    = 2351;
    private const int MithrilBarId = 2359;
    private const int AdamantBarId = 2361;
    private const int RuneBarId    = 2363;

    // Dart tip output IDs
    private const int BronzeDartTipId  = 819;
    private const int IronDartTipId    = 820;
    private const int SteelDartTipId   = 821;
    private const int MithrilDartTipId = 822;
    private const int AdamantDartTipId = 823;
    private const int RuneDartTipId    = 824;

    public SmithingServiceTests()
    {
        _sut = new SmithingService(
            _dataFetchMock.Object,
            _profitCalcMock.Object,
            _priceRecommendationMock.Object,
            _loggerMock.Object);
    }

    #region GetCannonballProfitsAsync

    [Fact]
    public async Task GetCannonballProfitsAsync_ValidPrices_Returns4OutputPerBar()
    {
        // Arrange
        var prices = CreatePrices(
            (SteelBarId, buyPrice: 1200, sellPrice: 1210, volume: 5000),
            (CannonballId, buyPrice: 300, sellPrice: 310, volume: 5000));

        SetupPriceData(prices);
        SetupItemPrice(SteelBarId, buyPrice: 1200, sellPrice: 1210);
        SetupItemPrice(CannonballId, buyPrice: 300, sellPrice: 310);

        // barPrice=1200, outputPrice=310 (RecommendedSellPrice), outputPerInput=4, quantity=5000
        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(1200, 310, 4, 5000))
            .Returns(new ProfitCalculation
            {
                ProfitPerUnit = 40,   // (310 * 4) - 1200 = 40
                TotalProfit   = 200_000,
                RoiPercent    = 3.33
            });

        // Act
        var result = await _sut.GetCannonballProfitsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(4, result[0].OutputPerInput);
        Assert.Equal(40, result[0].ProfitPerUnit);
        Assert.Equal(200_000, result[0].TotalProfit);
        Assert.Equal(CannonballId, result[0].ItemId);
    }

    [Fact]
    public async Task GetCannonballProfitsAsync_CorrectBarIds_SteelBarOnly()
    {
        // Arrange — only steel bar entry, confirming cannonball uses steel (2353), not other bars
        var prices = CreatePrices(
            (SteelBarId, buyPrice: 1200, sellPrice: 1210, volume: 3000),
            (CannonballId, buyPrice: 300, sellPrice: 305, volume: 3000));

        SetupPriceData(prices);
        SetupItemPrice(SteelBarId, buyPrice: 1200, sellPrice: 1210);
        SetupItemPrice(CannonballId, buyPrice: 300, sellPrice: 305);

        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ProfitCalculation { ProfitPerUnit = 20, RoiPercent = 1.67 });

        // Act
        var result = await _sut.GetCannonballProfitsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SteelBarId, result[0].BarId);
        Assert.Equal("Steel bar", result[0].BarName);
        Assert.Equal("Cannonball", result[0].Name);
        Assert.Equal(1200, result[0].BarPrice);
    }

    [Fact]
    public async Task GetCannonballProfitsAsync_ZeroVolume_Excluded()
    {
        // Arrange
        var prices = CreatePrices(
            (SteelBarId, buyPrice: 1200, sellPrice: 1210, volume: 5000),
            (CannonballId, buyPrice: 300, sellPrice: 310, volume: 0)); // zero volume → excluded

        SetupPriceData(prices);

        // Act
        var result = await _sut.GetCannonballProfitsAsync();

        // Assert
        Assert.Empty(result);
        _profitCalcMock.Verify(
            x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCannonballProfitsAsync_MissingBarPriceData_Excluded()
    {
        // Arrange — cannonball price exists but steel bar price is absent
        var prices = CreatePrices(
            (CannonballId, buyPrice: 300, sellPrice: 310, volume: 5000));

        SetupPriceData(prices);

        // Act
        var result = await _sut.GetCannonballProfitsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCannonballProfitsAsync_MembersFlag_IsTrue()
    {
        // Arrange — cannonball smithing requires Dwarf Cannon (members quest)
        var prices = CreatePrices(
            (SteelBarId, buyPrice: 1200, sellPrice: 1210, volume: 2000),
            (CannonballId, buyPrice: 300, sellPrice: 310, volume: 2000));

        SetupPriceData(prices);
        SetupItemPrice(SteelBarId, buyPrice: 1200, sellPrice: 1210);
        SetupItemPrice(CannonballId, buyPrice: 300, sellPrice: 310);

        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ProfitCalculation { ProfitPerUnit = 40 });

        // Act
        var result = await _sut.GetCannonballProfitsAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result[0].Members);
    }

    #endregion

    #region GetDartTipProfitsAsync

    [Fact]
    public async Task GetDartTipProfitsAsync_ValidPrices_Returns10OutputPerBar()
    {
        // Arrange — single bar/dart-tip pair
        var prices = CreatePrices(
            (SteelBarId, buyPrice: 1200, sellPrice: 1210, volume: 8000),
            (SteelDartTipId, buyPrice: 90, sellPrice: 95, volume: 8000));

        SetupPriceData(prices);
        SetupItemPrice(SteelBarId, buyPrice: 1200, sellPrice: 1210);
        SetupItemPrice(SteelDartTipId, buyPrice: 90, sellPrice: 95);

        // barPrice=1200, outputPrice=95 (RecommendedSellPrice), outputPerInput=10, quantity=8000
        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(1200, 95, 10, 8000))
            .Returns(new ProfitCalculation
            {
                ProfitPerUnit = -250,  // (95 * 10) - 1200 = -250
                TotalProfit   = -2_000_000,
                RoiPercent    = -20.83
            });

        // Act
        var result = await _sut.GetDartTipProfitsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(10, result[0].OutputPerInput);
        Assert.Equal(SteelDartTipId, result[0].ItemId);
    }

    [Fact]
    public async Task GetDartTipProfitsAsync_AllItems_AreMembersOnly()
    {
        // Arrange — all 6 bar/dart-tip pairs
        var prices = CreateAllDartTipPrices();
        SetupPriceData(prices);
        SetupAllDartTipItemPrices();

        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ProfitCalculation { ProfitPerUnit = 100, RoiPercent = 5.0 });

        // Act
        var result = await _sut.GetDartTipProfitsAsync();

        // Assert
        Assert.Equal(6, result.Count);
        Assert.All(result, item => Assert.True(item.Members));
    }

    [Fact]
    public async Task GetDartTipProfitsAsync_AllSixBarTypes_Processed()
    {
        // Arrange — all 6 recipes must be evaluated and returned
        var prices = CreateAllDartTipPrices();
        SetupPriceData(prices);
        SetupAllDartTipItemPrices();

        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ProfitCalculation { ProfitPerUnit = 50, RoiPercent = 2.5 });

        // Act
        var result = await _sut.GetDartTipProfitsAsync();

        // Assert — one result per bar type, all 6 bar IDs present
        Assert.Equal(6, result.Count);
        var barIds = result.Select(x => x.BarId).ToHashSet();
        Assert.Contains(BronzeBarId,  barIds);
        Assert.Contains(IronBarId,    barIds);
        Assert.Contains(SteelBarId,   barIds);
        Assert.Contains(MithrilBarId, barIds);
        Assert.Contains(AdamantBarId, barIds);
        Assert.Contains(RuneBarId,    barIds);
    }

    [Fact]
    public async Task GetDartTipProfitsAsync_ZeroVolume_Excluded()
    {
        // Arrange — bronze dart tip has zero volume; other 5 have valid data
        var prices = CreateAllDartTipPrices(zeroBronzeVolume: true);
        SetupPriceData(prices);
        SetupAllDartTipItemPrices();

        _profitCalcMock
            .Setup(x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ProfitCalculation { ProfitPerUnit = 50 });

        // Act
        var result = await _sut.GetDartTipProfitsAsync();

        // Assert — 5 results, bronze dart tip excluded
        Assert.Equal(5, result.Count);
        Assert.DoesNotContain(result, x => x.BarId == BronzeBarId);
    }

    [Fact]
    public async Task GetDartTipProfitsAsync_NoPriceData_ReturnsEmpty()
    {
        // Arrange
        var prices = new Dictionary<int, ItemPriceData>();
        SetupPriceData(prices);

        // Act
        var result = await _sut.GetDartTipProfitsAsync();

        // Assert
        Assert.Empty(result);
        _profitCalcMock.Verify(
            x => x.CalculateMultiOutputProfit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    #region Test Helpers

    private static Dictionary<int, ItemPriceData> CreatePrices(
        params (int id, int buyPrice, int sellPrice, int volume)[] items)
    {
        return items.ToDictionary(
            x => x.id,
            x => new ItemPriceData
            {
                ItemId = x.id,
                LatestBuyPrice = x.buyPrice,
                LatestSellPrice = x.sellPrice,
                TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
                {
                    [TimeWindow.OneHour] = new()
                    {
                        AvgBuyPrice = x.buyPrice,
                        AvgSellPrice = x.sellPrice,
                        BuyVolume = x.volume / 2,
                        SellVolume = x.volume / 2
                    },
                    [TimeWindow.SixHour] = new()
                    {
                        AvgBuyPrice = x.buyPrice,
                        AvgSellPrice = x.sellPrice,
                        BuyVolume = x.volume / 2,
                        SellVolume = x.volume / 2
                    },
                    // Must include TwentyFourHour — Volume24Hr reads from this window
                    [TimeWindow.TwentyFourHour] = new()
                    {
                        AvgBuyPrice = x.buyPrice,
                        AvgSellPrice = x.sellPrice,
                        BuyVolume = x.volume / 2,
                        SellVolume = x.volume / 2
                    }
                }
            });
    }

    private static Dictionary<int, ItemPriceData> CreateAllDartTipPrices(bool zeroBronzeVolume = false)
    {
        return CreatePrices(
            (BronzeBarId,  buyPrice: 200,  sellPrice: 205,  volume: zeroBronzeVolume ? 0 : 1000),
            (IronBarId,    buyPrice: 400,  sellPrice: 410,  volume: 2000),
            (SteelBarId,   buyPrice: 1200, sellPrice: 1210, volume: 5000),
            (MithrilBarId, buyPrice: 3000, sellPrice: 3050, volume: 3000),
            (AdamantBarId, buyPrice: 6000, sellPrice: 6100, volume: 2000),
            (RuneBarId,    buyPrice: 12000, sellPrice: 12100, volume: 1000),
            (BronzeDartTipId,  buyPrice: 60,  sellPrice: 65,  volume: zeroBronzeVolume ? 0 : 1000),
            (IronDartTipId,    buyPrice: 80,  sellPrice: 85,  volume: 2000),
            (SteelDartTipId,   buyPrice: 90,  sellPrice: 95,  volume: 5000),
            (MithrilDartTipId, buyPrice: 200, sellPrice: 210, volume: 3000),
            (AdamantDartTipId, buyPrice: 400, sellPrice: 420, volume: 2000),
            (RuneDartTipId,    buyPrice: 800, sellPrice: 850, volume: 1000));
    }

    private void SetupPriceData(Dictionary<int, ItemPriceData> prices)
    {
        _dataFetchMock
            .Setup(x => x.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemPriceData>)prices);
    }

    private void SetupItemPrice(int itemId, int buyPrice, int sellPrice)
    {
        _priceRecommendationMock
            .Setup(x => x.CalculateRecommendedPrices(It.Is<ItemPriceData>(p => p.ItemId == itemId)))
            .Returns(new PriceRecommendation
            {
                RecommendedBuyPrice  = buyPrice,
                RecommendedSellPrice = sellPrice,
                WindowsUsedForBuy    = 2,
                WindowsUsedForSell   = 2
            });
    }

    private void SetupAllDartTipItemPrices()
    {
        SetupItemPrice(BronzeBarId,  buyPrice: 200,   sellPrice: 205);
        SetupItemPrice(IronBarId,    buyPrice: 400,   sellPrice: 410);
        SetupItemPrice(SteelBarId,   buyPrice: 1200,  sellPrice: 1210);
        SetupItemPrice(MithrilBarId, buyPrice: 3000,  sellPrice: 3050);
        SetupItemPrice(AdamantBarId, buyPrice: 6000,  sellPrice: 6100);
        SetupItemPrice(RuneBarId,    buyPrice: 12000, sellPrice: 12100);
        SetupItemPrice(BronzeDartTipId,  buyPrice: 60,  sellPrice: 65);
        SetupItemPrice(IronDartTipId,    buyPrice: 80,  sellPrice: 85);
        SetupItemPrice(SteelDartTipId,   buyPrice: 90,  sellPrice: 95);
        SetupItemPrice(MithrilDartTipId, buyPrice: 200, sellPrice: 210);
        SetupItemPrice(AdamantDartTipId, buyPrice: 400, sellPrice: 420);
        SetupItemPrice(RuneDartTipId,    buyPrice: 800, sellPrice: 850);
    }

    #endregion
}
