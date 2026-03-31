using Microsoft.Extensions.Logging;
using Moq;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.Services;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.UnitTests.Core.Services;

public class HerbloreServiceTests
{
    private readonly Mock<IDataFetchService> _dataFetchMock = new();
    private readonly Mock<IPriceRecommendationService> _priceRecommendationMock = new();
    private readonly Mock<ILogger<HerbloreService>> _loggerMock = new();
    private readonly HerbloreService _sut;

    // Ranarr weed IDs
    private const int GrimyRanarrId  = 207;
    private const int CleanRanarrId  = 257;
    private const int RanarrUnfId    = 99;
    private const int PrayerPotionId = 139;
    private const int SnapeGrassId   = 231;
    private const int VialOfWaterId  = 227;

    public HerbloreServiceTests()
    {
        _sut = new HerbloreService(
            _dataFetchMock.Object,
            _priceRecommendationMock.Object,
            _loggerMock.Object);
    }

    #region GetCleaningProfitsAsync

    [Fact]
    public async Task GetCleaningProfitsAsync_ValidPrices_CalculatesCorrectProfit()
    {
        // Arrange: grimy @ 5000, clean sells @ 6000 → profit = 1000
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (CleanRanarrId, 6000, 8000));

        SetupPrices(prices);
        SetupRecommendation(GrimyRanarrId, buyPrice: 5000, sellPrice: 5100);
        SetupRecommendation(CleanRanarrId, buyPrice: 5900, sellPrice: 6000);

        // Act
        var result = await _sut.GetCleaningProfitsAsync();

        // Assert
        var ranarr = result.Single(x => x.HerbName == "Ranarr weed");
        Assert.Equal(1000, ranarr.ProfitPerUnit);
        Assert.Equal(5000, ranarr.HerbPrice);
        Assert.Equal(6000, ranarr.OutputPrice);
        Assert.Null(ranarr.SecondaryPrice);
        Assert.Equal(HerbloreMethod.Cleaning, ranarr.Method);
    }

    [Fact]
    public async Task GetCleaningProfitsAsync_ZeroVolumeCleanHerb_IsExcluded()
    {
        // Arrange: clean herb has zero volume
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (CleanRanarrId, 6000, 0));

        SetupPrices(prices);
        SetupRecommendation(GrimyRanarrId, buyPrice: 5000, sellPrice: 5100);
        SetupRecommendation(CleanRanarrId, buyPrice: 5900, sellPrice: 6000);

        // Act
        var result = await _sut.GetCleaningProfitsAsync();

        // Assert
        Assert.DoesNotContain(result, x => x.HerbName == "Ranarr weed");
    }

    [Fact]
    public async Task GetCleaningProfitsAsync_AllItemsMarkedAsMembers()
    {
        // Arrange: use one herb
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (CleanRanarrId, 6000, 8000));

        SetupPrices(prices);
        SetupRecommendation(GrimyRanarrId, buyPrice: 5000, sellPrice: 5100);
        SetupRecommendation(CleanRanarrId, buyPrice: 5900, sellPrice: 6000);

        // Act
        var result = await _sut.GetCleaningProfitsAsync();

        // Assert
        Assert.All(result, x => Assert.True(x.Members));
    }

    [Fact]
    public async Task GetCleaningProfitsAsync_MissingGrimyPrice_HerbExcluded()
    {
        // Arrange: grimy herb has no price data
        var prices = BuildPrices(
            (CleanRanarrId, 6000, 8000));

        SetupPrices(prices);
        SetupRecommendation(CleanRanarrId, buyPrice: 5900, sellPrice: 6000);

        // Act
        var result = await _sut.GetCleaningProfitsAsync();

        // Assert
        Assert.DoesNotContain(result, x => x.HerbName == "Ranarr weed");
    }

    #endregion

    #region GetFullProcessProfitsAsync

    [Fact]
    public async Task GetFullProcessProfitsAsync_ValidPrices_IncludesVialCost()
    {
        // Arrange: grimy @ 5000, vial @ 3, unf potion sells @ 5500 → profit = 5500 - 5000 - 3 = 497
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (VialOfWaterId, 3, 50000),
            (RanarrUnfId,   5500, 8000));

        SetupPrices(prices);
        SetupRecommendation(GrimyRanarrId, buyPrice: 5000, sellPrice: 5100);
        SetupRecommendation(VialOfWaterId, buyPrice: 3,    sellPrice: 4);
        SetupRecommendation(RanarrUnfId,   buyPrice: 5400, sellPrice: 5500);

        // Act
        var result = await _sut.GetFullProcessProfitsAsync();

        // Assert
        var ranarr = result.Single(x => x.HerbName == "Ranarr weed");
        Assert.Equal(497, ranarr.ProfitPerUnit);   // 5500 - 5000 - 3
        Assert.Equal(5000, ranarr.HerbPrice);
        Assert.Equal(3, ranarr.SecondaryPrice);    // vial price
        Assert.Equal(5500, ranarr.OutputPrice);
        Assert.Equal(HerbloreMethod.FullProcess, ranarr.Method);
    }

    [Fact]
    public async Task GetFullProcessProfitsAsync_MissingVialPrice_ReturnsEmpty()
    {
        // Arrange: no vial of water price data
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (RanarrUnfId,   5500, 8000));

        SetupPrices(prices);

        // Act
        var result = await _sut.GetFullProcessProfitsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFullProcessProfitsAsync_ZeroVolumeUnfPotion_IsExcluded()
    {
        // Arrange: unfinished potion has zero volume
        var prices = BuildPrices(
            (GrimyRanarrId, 5000, 10000),
            (VialOfWaterId, 3,    50000),
            (RanarrUnfId,   5500, 0));

        SetupPrices(prices);
        SetupRecommendation(GrimyRanarrId, buyPrice: 5000, sellPrice: 5100);
        SetupRecommendation(VialOfWaterId, buyPrice: 3,    sellPrice: 4);
        SetupRecommendation(RanarrUnfId,   buyPrice: 5400, sellPrice: 5500);

        // Act
        var result = await _sut.GetFullProcessProfitsAsync();

        // Assert
        Assert.DoesNotContain(result, x => x.HerbName == "Ranarr weed");
    }

    #endregion

    #region GetPotionMakingProfitsAsync

    [Fact]
    public async Task GetPotionMakingProfitsAsync_ValidPrices_CalculatesCorrectProfit()
    {
        // Arrange: clean @ 6000, snape grass @ 400, prayer pot sells @ 10000 → profit = 3600
        var prices = BuildPrices(
            (CleanRanarrId,  6000,  8000),
            (SnapeGrassId,    400, 20000),
            (PrayerPotionId, 10000, 5000));

        SetupPrices(prices);
        SetupRecommendation(CleanRanarrId,  buyPrice: 6000,  sellPrice: 6100);
        SetupRecommendation(SnapeGrassId,   buyPrice: 400,   sellPrice: 420);
        SetupRecommendation(PrayerPotionId, buyPrice: 9800,  sellPrice: 10000);

        // Act
        var result = await _sut.GetPotionMakingProfitsAsync();

        // Assert
        var ranarr = result.Single(x => x.HerbName == "Ranarr weed");
        Assert.Equal(3600, ranarr.ProfitPerUnit);  // 10000 - 6000 - 400
        Assert.Equal(6000, ranarr.HerbPrice);
        Assert.Equal(400,  ranarr.SecondaryPrice);
        Assert.Equal(10000, ranarr.OutputPrice);
        Assert.Equal(HerbloreMethod.PotionMaking, ranarr.Method);
    }

    [Fact]
    public async Task GetPotionMakingProfitsAsync_MissingSecondaryPrice_HerbExcluded()
    {
        // Arrange: no snape grass price data
        var prices = BuildPrices(
            (CleanRanarrId,  6000,  8000),
            (PrayerPotionId, 10000, 5000));

        SetupPrices(prices);
        SetupRecommendation(CleanRanarrId,  buyPrice: 6000,  sellPrice: 6100);
        SetupRecommendation(PrayerPotionId, buyPrice: 9800,  sellPrice: 10000);

        // Act
        var result = await _sut.GetPotionMakingProfitsAsync();

        // Assert
        Assert.DoesNotContain(result, x => x.HerbName == "Ranarr weed");
    }

    [Fact]
    public async Task GetPotionMakingProfitsAsync_ResultsSortedByProfitDescending()
    {
        // Arrange: two herbs — Ranarr (high profit) and Guam (low profit)
        const int GrimyGuamId  = 199;
        const int CleanGuamId  = 249;
        const int AttackPotId  = 121;
        const int EyeOfNewtId  = 221;

        var prices = BuildPrices(
            (CleanRanarrId,  6000,  8000),
            (SnapeGrassId,    400, 20000),
            (PrayerPotionId, 10000, 5000),
            (CleanGuamId,     100, 10000),
            (EyeOfNewtId,      50, 30000),
            (AttackPotId,      300, 4000));

        SetupPrices(prices);
        SetupRecommendation(CleanRanarrId,  buyPrice: 6000,  sellPrice: 6100);
        SetupRecommendation(SnapeGrassId,   buyPrice: 400,   sellPrice: 420);
        SetupRecommendation(PrayerPotionId, buyPrice: 9800,  sellPrice: 10000);
        SetupRecommendation(CleanGuamId,    buyPrice: 100,   sellPrice: 110);
        SetupRecommendation(EyeOfNewtId,    buyPrice: 50,    sellPrice: 55);
        SetupRecommendation(AttackPotId,    buyPrice: 280,   sellPrice: 300);

        // Act
        var result = (await _sut.GetPotionMakingProfitsAsync()).ToList();

        // Assert: Ranarr (profit 3600) should come before Guam (profit 150)
        var ranarr = result.First(x => x.HerbName == "Ranarr weed");
        var guam   = result.First(x => x.HerbName == "Guam leaf");
        Assert.True(result.IndexOf(ranarr) < result.IndexOf(guam));
    }

    #endregion

    #region Test Helpers

    private static Dictionary<int, ItemPriceData> BuildPrices(
        params (int id, int price, int volume)[] items)
    {
        return items.ToDictionary(
            x => x.id,
            x => new ItemPriceData
            {
                ItemId = x.id,
                LatestBuyPrice = x.price,
                TimeWindows = new Dictionary<TimeWindow, TimeWindowPrice>
                {
                    [TimeWindow.OneHour] = new()
                    {
                        AvgBuyPrice = x.price, AvgSellPrice = x.price - 5,
                        BuyVolume = x.volume / 2, SellVolume = x.volume / 2
                    },
                    [TimeWindow.SixHour] = new()
                    {
                        AvgBuyPrice = x.price, AvgSellPrice = x.price - 5,
                        BuyVolume = x.volume / 2, SellVolume = x.volume / 2
                    },
                    [TimeWindow.TwentyFourHour] = new()
                    {
                        AvgBuyPrice = x.price, AvgSellPrice = x.price - 5,
                        BuyVolume = x.volume / 2, SellVolume = x.volume / 2
                    }
                }
            });
    }

    private void SetupPrices(Dictionary<int, ItemPriceData> prices)
    {
        _dataFetchMock
            .Setup(x => x.GetCompletePriceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemPriceData>)prices);
        _dataFetchMock
            .Setup(x => x.GetMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<int, ItemMapping>)new Dictionary<int, ItemMapping>());
    }

    private void SetupRecommendation(int itemId, int buyPrice, int sellPrice)
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

    #endregion
}
