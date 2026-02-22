using Microsoft.Extensions.Logging;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Calculates smithing profitability for cannonballs and dart tips.
/// Fetches live price data via IDataFetchService and uses hardcoded recipe
/// definitions to evaluate profit per bar smelted.
/// </summary>
public class SmithingService : ISmithingService
{
    // Cannonballs: steel bars only — this is the only bar type supported in OSRS.
    private static readonly SmithingRecipe[] CannonballRecipes =
    [
        new(OutputItemId: 2, OutputName: "Cannonball", BarId: 2353, BarName: "Steel bar",
            OutputPerInput: 4, Members: true, Type: SmithingType.Cannonball)
    ];

    // Dart tips: all 6 bar types, all members-only, 10 tips per bar.
    private static readonly SmithingRecipe[] DartTipRecipes =
    [
        new(OutputItemId: 819, OutputName: "Bronze dart tip", BarId: 2349, BarName: "Bronze bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip),
        new(OutputItemId: 820, OutputName: "Iron dart tip",   BarId: 2351, BarName: "Iron bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip),
        new(OutputItemId: 821, OutputName: "Steel dart tip",  BarId: 2353, BarName: "Steel bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip),
        new(OutputItemId: 822, OutputName: "Mithril dart tip", BarId: 2359, BarName: "Mithril bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip),
        new(OutputItemId: 823, OutputName: "Adamant dart tip", BarId: 2361, BarName: "Adamant bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip),
        new(OutputItemId: 824, OutputName: "Rune dart tip",   BarId: 2363, BarName: "Rune bar",
            OutputPerInput: 10, Members: true, Type: SmithingType.DartTip)
    ];

    private readonly IDataFetchService _dataFetchService;
    private readonly IProfitCalculationService _profitCalcService;
    private readonly IPriceRecommendationService _priceRecommendationService;
    private readonly ILogger<SmithingService> _logger;

    public SmithingService(
        IDataFetchService dataFetchService,
        IProfitCalculationService profitCalcService,
        IPriceRecommendationService priceRecommendationService,
        ILogger<SmithingService> logger)
    {
        _dataFetchService = dataFetchService;
        _profitCalcService = profitCalcService;
        _priceRecommendationService = priceRecommendationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SmithingItem>> GetCannonballProfitsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating cannonball smithing profitability...");
        return await CalculateSmithingProfitsAsync(CannonballRecipes, cancellationToken);
    }

    public async Task<IReadOnlyList<SmithingItem>> GetDartTipProfitsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating dart tip smithing profitability...");
        return await CalculateSmithingProfitsAsync(DartTipRecipes, cancellationToken);
    }

    #region Private Helpers

    private async Task<IReadOnlyList<SmithingItem>> CalculateSmithingProfitsAsync(
        IEnumerable<SmithingRecipe> recipes,
        CancellationToken cancellationToken)
    {
        var prices = await _dataFetchService.GetCompletePriceDataAsync(cancellationToken);

        var results = new List<SmithingItem>();

        foreach (var recipe in recipes)
        {
            // Both bar and output must have price data
            if (!prices.TryGetValue(recipe.BarId, out var barPriceData))
            {
                _logger.LogDebug("No price data for bar {BarId} ({BarName}), skipping",
                    recipe.BarId, recipe.BarName);
                continue;
            }

            if (!prices.TryGetValue(recipe.OutputItemId, out var outputPriceData))
            {
                _logger.LogDebug("No price data for output {OutputId} ({OutputName}), skipping",
                    recipe.OutputItemId, recipe.OutputName);
                continue;
            }

            // Exclude outputs with zero 24h volume — no active market
            var volume = outputPriceData.Volume24Hr;
            if (volume <= 0)
            {
                _logger.LogDebug("Zero volume for {OutputName}, skipping", recipe.OutputName);
                continue;
            }

            // Bar: we buy bars → use RecommendedBuyPrice (cheapest price to fill a buy order)
            var barRecommendation = _priceRecommendationService.CalculateRecommendedPrices(barPriceData);
            var barPrice = barRecommendation.RecommendedBuyPrice;
            if (barPrice <= 0)
                continue;

            // Output: we sell outputs → use RecommendedSellPrice (realistic sell order price)
            var outputRecommendation = _priceRecommendationService.CalculateRecommendedPrices(outputPriceData);
            var outputPrice = outputRecommendation.RecommendedSellPrice;
            if (outputPrice <= 0)
                continue;

            var profitCalc = _profitCalcService.CalculateMultiOutputProfit(
                inputPrice: barPrice,
                outputPrice: outputPrice,
                outputPerInput: recipe.OutputPerInput,
                maxQuantity: volume);

            results.Add(new SmithingItem
            {
                ItemId = recipe.OutputItemId,
                Name = recipe.OutputName,
                Type = recipe.Type,
                Members = recipe.Members,
                BarName = recipe.BarName,
                BarId = recipe.BarId,
                BarPrice = barPrice,
                OutputPrice = outputPrice,
                OutputPerInput = recipe.OutputPerInput,
                ProfitPerUnit = profitCalc.ProfitPerUnit,
                TotalProfit = profitCalc.TotalProfit,
                Volume24Hr = volume,
                RoiPercent = profitCalc.RoiPercent
            });
        }

        var sorted = results.OrderByDescending(x => x.ProfitPerUnit).ToList();

        _logger.LogInformation(
            "Evaluated {Total} smithing recipes, {Profitable} profitable",
            sorted.Count,
            sorted.Count(x => x.IsProfitable));

        return sorted;
    }

    /// <summary>Defines a single smithing input→output relationship.</summary>
    private record struct SmithingRecipe(
        int OutputItemId,
        string OutputName,
        int BarId,
        string BarName,
        int OutputPerInput,
        bool Members,
        SmithingType Type);

    #endregion
}
