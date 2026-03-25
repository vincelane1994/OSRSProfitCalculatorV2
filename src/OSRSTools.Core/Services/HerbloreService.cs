using Microsoft.Extensions.Logging;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Calculates herblore profitability for cleaning herbs, making unfinished potions,
/// and making finished potions. Uses hardcoded recipes for 14 tradeable herbs
/// and fetches live prices via IDataFetchService.
/// </summary>
public class HerbloreService : IHerbloreService
{
    private const int VialOfWaterId = 227;

    private static readonly HerbloreRecipe[] Recipes =
    [
        new("Guam leaf",   199, 249, "Guam leaf",   91,   "Guam potion (unf)",         121,  "Attack potion(3)",    221, "Eye of newt",        true),
        new("Marrentill",  201, 251, "Marrentill",  93,   "Marrentill potion (unf)",   175,  "Antipoison(3)",       235, "Unicorn horn dust",  true),
        new("Tarromin",    203, 253, "Tarromin",    95,   "Tarromin potion (unf)",     115,  "Strength potion(3)",  225, "Limpwurt root",      true),
        new("Harralander", 205, 255, "Harralander", 97,   "Harralander potion (unf)",  127,  "Restore potion(3)",   223, "Red spiders' eggs",  true),
        new("Ranarr weed", 207, 257, "Ranarr weed", 99,   "Ranarr potion (unf)",       139,  "Prayer potion(3)",    231, "Snape grass",        true),
        new("Toadflax",   3049, 2998, "Toadflax",  3002, "Toadflax potion (unf)",    6687,  "Saradomin brew(3)",  6693, "Crushed nest",       true),
        new("Irit leaf",   209, 259, "Irit leaf",  101,  "Irit potion (unf)",          145,  "Super attack(3)",     221, "Eye of newt",        true),
        new("Avantoe",     211, 261, "Avantoe",    103,  "Avantoe potion (unf)",      3018,  "Super energy(3)",    2970, "Mort myre fungus",   true),
        new("Kwuarm",      213, 263, "Kwuarm",     105,  "Kwuarm potion (unf)",        157,  "Super strength(3)",   225, "Limpwurt root",      true),
        new("Snapdragon", 3051, 3000, "Snapdragon",3004, "Snapdragon potion (unf)",  3026,  "Super restore(3)",    223, "Red spiders' eggs",  true),
        new("Cadantine",   215, 265, "Cadantine",  107,  "Cadantine potion (unf)",     163,  "Super defence(3)",    239, "White berries",      true),
        new("Lantadyme",  2485, 2481, "Lantadyme", 2483, "Lantadyme potion (unf)",   2454,  "Antifire potion(3)",  241, "Dragon scale dust",  true),
        new("Dwarf weed",  217, 267, "Dwarf weed", 109,  "Dwarf weed potion (unf)",   169,  "Ranging potion(3)",   245, "Wine of zamorak",    true),
        new("Torstol",     219, 269, "Torstol",    111,  "Torstol potion (unf)",       189,  "Zamorak brew(3)",     247, "Jangerberries",      true),
    ];

    private readonly IDataFetchService _dataFetchService;
    private readonly IPriceRecommendationService _priceRecommendationService;
    private readonly ILogger<HerbloreService> _logger;

    public HerbloreService(
        IDataFetchService dataFetchService,
        IPriceRecommendationService priceRecommendationService,
        ILogger<HerbloreService> logger)
    {
        _dataFetchService = dataFetchService;
        _priceRecommendationService = priceRecommendationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HerbloreItem>> GetCleaningProfitsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating herb cleaning profitability...");
        var prices = await _dataFetchService.GetCompletePriceDataAsync(cancellationToken);
        var results = new List<HerbloreItem>();

        foreach (var recipe in Recipes)
        {
            if (!prices.TryGetValue(recipe.GrimyHerbId, out var grimyData) ||
                !prices.TryGetValue(recipe.CleanHerbId, out var cleanData))
                continue;

            var volume = cleanData.Volume24Hr;
            if (volume <= 0)
                continue;

            var grimyRec = _priceRecommendationService.CalculateRecommendedPrices(grimyData);
            var cleanRec = _priceRecommendationService.CalculateRecommendedPrices(cleanData);

            var grimyBuyPrice = grimyRec.RecommendedBuyPrice;
            var cleanSellPrice = cleanRec.RecommendedSellPrice;

            if (grimyBuyPrice <= 0 || cleanSellPrice <= 0)
                continue;

            var profit = cleanSellPrice - grimyBuyPrice;
            var roi = (double)profit / grimyBuyPrice * 100;

            results.Add(new HerbloreItem
            {
                ItemId = recipe.CleanHerbId,
                Name = recipe.CleanHerbName,
                Method = HerbloreMethod.Cleaning,
                HerbName = recipe.HerbName,
                HerbPrice = grimyBuyPrice,
                SecondaryPrice = null,
                OutputPrice = cleanSellPrice,
                ProfitPerUnit = profit,
                Volume24Hr = volume,
                RoiPercent = roi,
                Members = recipe.Members
            });
        }

        return results.OrderByDescending(x => x.ProfitPerUnit).ToList();
    }

    public async Task<IReadOnlyList<HerbloreItem>> GetFullProcessProfitsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating full process (grimy → unfinished potion) profitability...");
        var prices = await _dataFetchService.GetCompletePriceDataAsync(cancellationToken);
        var results = new List<HerbloreItem>();

        if (!prices.TryGetValue(VialOfWaterId, out var vialData))
        {
            _logger.LogWarning("No price data for Vial of Water (ID {Id}), returning empty results", VialOfWaterId);
            return results;
        }

        var vialRec = _priceRecommendationService.CalculateRecommendedPrices(vialData);
        var vialBuyPrice = vialRec.RecommendedBuyPrice;
        if (vialBuyPrice <= 0)
            return results;

        foreach (var recipe in Recipes)
        {
            if (!prices.TryGetValue(recipe.GrimyHerbId, out var grimyData) ||
                !prices.TryGetValue(recipe.UnfinishedPotionId, out var unfData))
                continue;

            var volume = unfData.Volume24Hr;
            if (volume <= 0)
                continue;

            var grimyRec = _priceRecommendationService.CalculateRecommendedPrices(grimyData);
            var unfRec = _priceRecommendationService.CalculateRecommendedPrices(unfData);

            var grimyBuyPrice = grimyRec.RecommendedBuyPrice;
            var unfSellPrice = unfRec.RecommendedSellPrice;

            if (grimyBuyPrice <= 0 || unfSellPrice <= 0)
                continue;

            var totalCost = grimyBuyPrice + vialBuyPrice;
            var profit = unfSellPrice - totalCost;
            var roi = (double)profit / totalCost * 100;

            results.Add(new HerbloreItem
            {
                ItemId = recipe.UnfinishedPotionId,
                Name = recipe.UnfPotionName,
                Method = HerbloreMethod.FullProcess,
                HerbName = recipe.HerbName,
                HerbPrice = grimyBuyPrice,
                SecondaryPrice = vialBuyPrice,
                OutputPrice = unfSellPrice,
                ProfitPerUnit = profit,
                Volume24Hr = volume,
                RoiPercent = roi,
                Members = recipe.Members
            });
        }

        return results.OrderByDescending(x => x.ProfitPerUnit).ToList();
    }

    public async Task<IReadOnlyList<HerbloreItem>> GetPotionMakingProfitsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating potion making (clean herb + secondary → potion) profitability...");
        var prices = await _dataFetchService.GetCompletePriceDataAsync(cancellationToken);
        var results = new List<HerbloreItem>();

        foreach (var recipe in Recipes)
        {
            if (!prices.TryGetValue(recipe.CleanHerbId, out var cleanData) ||
                !prices.TryGetValue(recipe.SecondaryId, out var secondaryData) ||
                !prices.TryGetValue(recipe.PotionId, out var potionData))
                continue;

            var volume = potionData.Volume24Hr;
            if (volume <= 0)
                continue;

            var cleanRec = _priceRecommendationService.CalculateRecommendedPrices(cleanData);
            var secondaryRec = _priceRecommendationService.CalculateRecommendedPrices(secondaryData);
            var potionRec = _priceRecommendationService.CalculateRecommendedPrices(potionData);

            var cleanBuyPrice = cleanRec.RecommendedBuyPrice;
            var secondaryBuyPrice = secondaryRec.RecommendedBuyPrice;
            var potionSellPrice = potionRec.RecommendedSellPrice;

            if (cleanBuyPrice <= 0 || secondaryBuyPrice <= 0 || potionSellPrice <= 0)
                continue;

            var totalCost = cleanBuyPrice + secondaryBuyPrice;
            var profit = potionSellPrice - totalCost;
            var roi = (double)profit / totalCost * 100;

            results.Add(new HerbloreItem
            {
                ItemId = recipe.PotionId,
                Name = recipe.PotionName,
                Method = HerbloreMethod.PotionMaking,
                HerbName = recipe.HerbName,
                HerbPrice = cleanBuyPrice,
                SecondaryPrice = secondaryBuyPrice,
                OutputPrice = potionSellPrice,
                ProfitPerUnit = profit,
                Volume24Hr = volume,
                RoiPercent = roi,
                Members = recipe.Members
            });
        }

        return results.OrderByDescending(x => x.ProfitPerUnit).ToList();
    }

    #region Private Helpers

    /// <summary>Defines a single herblore input→output recipe for all three methods.</summary>
    private record struct HerbloreRecipe(
        string HerbName,
        int GrimyHerbId,
        int CleanHerbId,
        string CleanHerbName,
        int UnfinishedPotionId,
        string UnfPotionName,
        int PotionId,
        string PotionName,
        int SecondaryId,
        string SecondaryName,
        bool Members);

    #endregion
}
