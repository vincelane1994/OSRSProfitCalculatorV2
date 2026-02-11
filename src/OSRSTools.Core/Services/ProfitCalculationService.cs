using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Services;

/// <summary>
/// Pure domain service for profit-related calculations.
/// All calculations are deterministic with no I/O.
/// </summary>
public class ProfitCalculationService : IProfitCalculationService
{
    private readonly TaxSettings _taxSettings;
    private readonly PriceWeightSettings _priceWeights;

    public ProfitCalculationService(
        IOptions<TaxSettings> taxSettings,
        IOptions<PriceWeightSettings> priceWeights)
    {
        _taxSettings = taxSettings.Value;
        _priceWeights = priceWeights.Value;
    }

    public TaxCalculation CalculateTax(int sellPrice)
    {
        return TaxCalculation.Calculate(sellPrice, _taxSettings.Rate, _taxSettings.Cap);
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

    public ProfitCalculation CalculateSimpleProfit(int buyPrice, int sellPrice, int maxQuantity)
    {
        if (maxQuantity <= 0 || buyPrice <= 0)
        {
            return default;
        }

        var profitPerUnit = sellPrice - buyPrice;
        var totalInvestment = (long)buyPrice * maxQuantity;
        var totalProfit = (long)profitPerUnit * maxQuantity;
        var roi = buyPrice > 0 ? (double)profitPerUnit / buyPrice * 100.0 : 0;

        return new ProfitCalculation
        {
            ProfitPerUnit = profitPerUnit,
            CostPerUnit = buyPrice,
            RevenuePerUnit = sellPrice,
            Quantity = maxQuantity,
            TotalInvestment = totalInvestment,
            TotalProfit = totalProfit,
            RoiPercent = Math.Round(roi, 2)
        };
    }

    public ProfitCalculation CalculateMultiOutputProfit(int inputPrice, int outputPrice, int outputPerInput, int maxQuantity)
    {
        if (maxQuantity <= 0 || inputPrice <= 0 || outputPerInput <= 0)
        {
            return default;
        }

        var revenuePerInput = outputPrice * outputPerInput;
        var profitPerInput = revenuePerInput - inputPrice;
        var totalInvestment = (long)inputPrice * maxQuantity;
        var totalProfit = (long)profitPerInput * maxQuantity;
        var roi = inputPrice > 0 ? (double)profitPerInput / inputPrice * 100.0 : 0;

        return new ProfitCalculation
        {
            ProfitPerUnit = profitPerInput,
            CostPerUnit = inputPrice,
            RevenuePerUnit = revenuePerInput,
            Quantity = maxQuantity,
            TotalInvestment = totalInvestment,
            TotalProfit = totalProfit,
            RoiPercent = Math.Round(roi, 2)
        };
    }

    public int CalculateMaxQuantity(int buyPrice, long maxInvestment, int buyLimit)
    {
        if (buyPrice <= 0) return 0;

        var quantityByCapital = (int)(maxInvestment / buyPrice);
        return Math.Min(buyLimit, quantityByCapital);
    }

    public double CalculateEstimatedFillHours(int buyLimit, int quantity, int volume24Hr, double buyLimitCycleHours)
    {
        // Floor hourly volume at 1 to prevent division by zero
        var hourlyVolume = Math.Max(volume24Hr / 24.0, 1.0);

        var buyHours = Math.Min((double)buyLimit / hourlyVolume, buyLimitCycleHours);
        var sellHours = Math.Min((double)quantity / hourlyVolume, buyLimitCycleHours);

        return buyHours + sellHours;
    }

    public double CalculateGpPerHour(long totalProfit, double estimatedFillHours)
    {
        if (estimatedFillHours <= 0) return 0;
        return totalProfit / estimatedFillHours;
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
