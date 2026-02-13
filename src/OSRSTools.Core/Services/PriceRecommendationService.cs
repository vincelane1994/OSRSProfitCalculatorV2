using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Services;

/// <summary>
/// Calculates weighted recommended buy/sell prices using configurable time window weights.
/// Dedicated to the flipping pipeline for independent evolution from general profit calculations.
/// </summary>
public class PriceRecommendationService : IPriceRecommendationService
{
    private readonly PriceWeightSettings _priceWeights;

    public PriceRecommendationService(IOptions<PriceWeightSettings> priceWeights)
    {
        _priceWeights = priceWeights.Value;
    }

    public PriceRecommendation CalculateRecommendedPrices(ItemPriceData priceData)
    {
        var weights = GetWindowWeights();

        // Buy price = weighted avg of instant-sell (low/AvgSellPrice) data
        var buyResult = CalculateWeightedPrice(priceData, weights, tw => tw.AvgSellPrice);

        // Sell price = weighted avg of instant-buy (high/AvgBuyPrice) data
        var sellResult = CalculateWeightedPrice(priceData, weights, tw => tw.AvgBuyPrice);

        return new PriceRecommendation
        {
            RecommendedBuyPrice = buyResult.Price,
            RecommendedSellPrice = sellResult.Price,
            WindowsUsedForBuy = buyResult.WindowsUsed,
            WindowsUsedForSell = sellResult.WindowsUsed
        };
    }

    #region Private Helpers

    private Dictionary<TimeWindow, double> GetWindowWeights()
    {
        return new Dictionary<TimeWindow, double>
        {
            { TimeWindow.FiveMinute, _priceWeights.FiveMinute },
            { TimeWindow.OneHour, _priceWeights.OneHour },
            { TimeWindow.SixHour, _priceWeights.SixHour },
            { TimeWindow.TwentyFourHour, _priceWeights.TwentyFourHour }
        };
    }

    private static (int Price, int WindowsUsed) CalculateWeightedPrice(
        ItemPriceData priceData,
        Dictionary<TimeWindow, double> weights,
        Func<TimeWindowPrice, int?> priceSelector)
    {
        var availableWindows = new List<(TimeWindow Window, int Price, double Weight)>();

        foreach (var (window, weight) in weights)
        {
            if (priceData.TimeWindows.TryGetValue(window, out var windowPrice))
            {
                var price = priceSelector(windowPrice);
                if (price.HasValue && price.Value > 0)
                {
                    availableWindows.Add((window, price.Value, weight));
                }
            }
        }

        if (availableWindows.Count == 0)
        {
            return (0, 0);
        }

        // Redistribute missing weights proportionally across available windows
        var totalAvailableWeight = availableWindows.Sum(w => w.Weight);
        if (totalAvailableWeight <= 0) return (0, 0);

        var weightedSum = availableWindows.Sum(w => w.Price * (w.Weight / totalAvailableWeight));

        return ((int)Math.Round(weightedSum), availableWindows.Count);
    }

    #endregion
}
