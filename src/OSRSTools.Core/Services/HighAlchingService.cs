using Microsoft.Extensions.Logging;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Evaluates all tradeable items for High Level Alchemy profitability.
/// Fetches item data via IDataFetchService, calculates profit using
/// IProfitCalculationService, and returns a ranked list of results.
/// </summary>
public class HighAlchingService : IHighAlchingService
{
    private const int NatureRuneItemId = 561;

    private readonly IDataFetchService _dataFetchService;
    private readonly IProfitCalculationService _profitCalcService;
    private readonly IPriceRecommendationService _priceRecommendationService;
    private readonly ILogger<HighAlchingService> _logger;

    public HighAlchingService(
        IDataFetchService dataFetchService,
        IProfitCalculationService profitCalcService,
        IPriceRecommendationService priceRecommendationService,
        ILogger<HighAlchingService> logger)
    {
        _dataFetchService = dataFetchService;
        _profitCalcService = profitCalcService;
        _priceRecommendationService = priceRecommendationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HighAlchItem>> GetProfitableItemsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating High Alchemy profitability for all items...");

        // Fetch mappings and price data concurrently
        var mappingsTask = _dataFetchService.GetMappingsAsync(cancellationToken);
        var pricesTask = _dataFetchService.GetCompletePriceDataAsync(cancellationToken);

        await Task.WhenAll(mappingsTask, pricesTask);

        var mappings = await mappingsTask;
        var prices = await pricesTask;

        // Determine nature rune cost
        var natureRuneCost = GetNatureRunePrice(prices);
        if (natureRuneCost <= 0)
        {
            _logger.LogWarning("Could not determine Nature Rune price, returning empty results");
            return Array.Empty<HighAlchItem>();
        }

        _logger.LogDebug("Nature Rune price: {Price} gp", natureRuneCost);

        var results = new List<HighAlchItem>();

        foreach (var (itemId, mapping) in mappings)
        {
            // Get high alch value
            var highAlchValue = await _dataFetchService.GetHighAlchValueAsync(itemId, cancellationToken);
            if (!highAlchValue.HasValue || highAlchValue.Value <= 0)
                continue;

            // Get price data
            if (!prices.TryGetValue(itemId, out var priceData))
                continue;

            // Exclude items with zero 24h volume
            if (priceData.Volume24Hr <= 0)
                continue;

            // Get recommended buy price
            var recommendation = _priceRecommendationService.CalculateRecommendedPrices(priceData);
            var buyPrice = recommendation.RecommendedBuyPrice;
            if (buyPrice <= 0)
                continue;

            // Calculate profit: highAlchValue - buyPrice - natureRuneCost
            var totalCost = buyPrice + natureRuneCost;
            var profit = highAlchValue.Value - totalCost;

            // Use CalculateSimpleProfit for ROI calculation
            var profitCalc = _profitCalcService.CalculateSimpleProfit(
                totalCost, highAlchValue.Value, mapping.BuyLimit);

            results.Add(new HighAlchItem
            {
                ItemId = itemId,
                Name = mapping.Name,
                Members = mapping.Members,
                BuyLimit = mapping.BuyLimit,
                BuyPrice = buyPrice,
                HighAlchValue = highAlchValue.Value,
                NatureRuneCost = natureRuneCost,
                Profit = profit,
                Volume24Hr = priceData.Volume24Hr,
                RoiPercent = profitCalc.RoiPercent
            });
        }

        var sorted = results
            .OrderByDescending(x => x.Profit)
            .ToList();

        _logger.LogInformation(
            "Evaluated {Total} items for High Alchemy, {Profitable} profitable",
            sorted.Count,
            sorted.Count(x => x.IsProfitable));

        return sorted;
    }

    #region Private Helpers

    private int GetNatureRunePrice(IReadOnlyDictionary<int, ItemPriceData> prices)
    {
        if (!prices.TryGetValue(NatureRuneItemId, out var natureRuneData))
            return 0;

        var recommendation = _priceRecommendationService.CalculateRecommendedPrices(natureRuneData);
        return recommendation.RecommendedBuyPrice > 0
            ? recommendation.RecommendedBuyPrice
            : natureRuneData.LatestBuyPrice ?? 0;
    }

    #endregion
}
