using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Services;

/// <summary>
/// Calculates complete flip profitability metrics for a single item.
/// Delegates tax, quantity, fill time, and GP/hr calculations to IProfitCalculationService.
/// </summary>
public class FlipCalculator : IFlipCalculator
{
    private readonly IProfitCalculationService _profitCalcService;

    public FlipCalculator(IProfitCalculationService profitCalcService)
    {
        _profitCalcService = profitCalcService;
    }

    public FlipCandidate CalculateFlip(
        int itemId,
        string name,
        bool members,
        int buyLimit,
        PriceRecommendation prices,
        ItemPriceData priceData,
        FlipSettings settings)
    {
        // 1. Margin = recommended sell - recommended buy (gross, before tax)
        var margin = prices.RecommendedSellPrice - prices.RecommendedBuyPrice;

        // 2. Tax on the sell price
        var tax = _profitCalcService.CalculateTax(prices.RecommendedSellPrice);

        // 3. Profit per unit = margin - tax
        var profitPerUnit = margin - tax.TaxAmount;

        // 4. Quantity = min(buyLimit, maxInvestment / buyPrice)
        var quantity = _profitCalcService.CalculateMaxQuantity(
            prices.RecommendedBuyPrice, settings.MaxInvestment, buyLimit);

        // 5. Total profit (long to prevent overflow)
        var totalProfit = (long)profitPerUnit * quantity;

        // 6. ROI = (profitPerUnit / buyPrice) * 100
        var roi = prices.RecommendedBuyPrice > 0
            ? Math.Round((double)profitPerUnit / prices.RecommendedBuyPrice * 100.0, 2)
            : 0.0;

        // 7. Fill time estimate (use best volume across all windows)
        var volume24Hr = priceData.Volume24Hr;
        var effectiveVolume = EstimateBestVolume24Hr(priceData);
        var fillHours = _profitCalcService.CalculateEstimatedFillHours(
            buyLimit, quantity, effectiveVolume, settings.BuyLimitCycleHours);

        // 8. GP/hr
        var gpPerHour = _profitCalcService.CalculateGpPerHour(totalProfit, fillHours);

        // 9. Profit per cycle = profitPerUnit × buyLimit
        var profitPerCycle = (long)profitPerUnit * buyLimit;

        // 10. Price volatility: compare 5m vs 6h averages
        var volatility = ComputeVolatility(priceData);

        return new FlipCandidate
        {
            ItemId = itemId,
            Name = name,
            Members = members,
            BuyLimit = buyLimit,
            RecommendedBuyPrice = prices.RecommendedBuyPrice,
            RecommendedSellPrice = prices.RecommendedSellPrice,
            Margin = margin,
            TaxAmount = tax.TaxAmount,
            ProfitPerUnit = profitPerUnit,
            Quantity = quantity,
            TotalProfit = totalProfit,
            RoiPercent = roi,
            GpPerHour = gpPerHour,
            EstimatedFillHours = fillHours,
            Volume24Hr = volume24Hr,
            HasSufficientData = prices.HasSufficientData,
            BuyWindowsUsed = prices.WindowsUsedForBuy,
            SellWindowsUsed = prices.WindowsUsedForSell,
            PriceVolatilityPercent = volatility,
            ProfitPerCycle = profitPerCycle,
            WindowPrices = BuildWindowSnapshots(priceData),
            ConfidenceRating = 0,
            FlipScore = 0
        };
    }

    private static readonly (TimeWindow Key, string Label)[] WindowOrder =
    [
        (TimeWindow.FiveMinute, "5 min"),
        (TimeWindow.OneHour, "1 hour"),
        (TimeWindow.SixHour, "6 hour"),
        (TimeWindow.TwentyFourHour, "24 hour")
    ];

    private static List<WindowPriceSnapshot> BuildWindowSnapshots(ItemPriceData data)
    {
        var snapshots = new List<WindowPriceSnapshot>();
        foreach (var (key, label) in WindowOrder)
        {
            if (data.TimeWindows.TryGetValue(key, out var wp))
            {
                snapshots.Add(new WindowPriceSnapshot
                {
                    Window = label,
                    AvgBuyPrice = wp.AvgBuyPrice,
                    AvgSellPrice = wp.AvgSellPrice,
                    BuyVolume = wp.BuyVolume,
                    SellVolume = wp.SellVolume
                });
            }
        }
        return snapshots;
    }

    private static int EstimateBestVolume24Hr(ItemPriceData data)
    {
        var multipliers = new (TimeWindow Window, int Multiplier)[]
        {
            (TimeWindow.FiveMinute, 288),
            (TimeWindow.OneHour, 24),
            (TimeWindow.SixHour, 4),
            (TimeWindow.TwentyFourHour, 1)
        };

        long maxVolume = 0;
        foreach (var (window, multiplier) in multipliers)
        {
            if (data.TimeWindows.TryGetValue(window, out var wp))
            {
                var extrapolated = (long)wp.TotalVolume * multiplier;
                if (extrapolated > maxVolume)
                    maxVolume = extrapolated;
            }
        }

        return (int)Math.Min(maxVolume, int.MaxValue);
    }

    private static double ComputeVolatility(ItemPriceData data)
    {
        var has5m = data.TimeWindows.TryGetValue(TimeWindow.FiveMinute, out var w5m);
        var has6h = data.TimeWindows.TryGetValue(TimeWindow.SixHour, out var w6h);
        if (!has5m || !has6h || w5m is null || w6h is null) return 0;

        double buyDev = 0, sellDev = 0;
        if (w5m.AvgBuyPrice.HasValue && w5m.AvgBuyPrice.Value > 0
            && w6h.AvgBuyPrice.HasValue && w6h.AvgBuyPrice.Value > 0)
        {
            buyDev = Math.Abs((double)(w5m.AvgBuyPrice.Value - w6h.AvgBuyPrice.Value)
                              / w6h.AvgBuyPrice.Value * 100.0);
        }

        if (w5m.AvgSellPrice.HasValue && w5m.AvgSellPrice.Value > 0
            && w6h.AvgSellPrice.HasValue && w6h.AvgSellPrice.Value > 0)
        {
            sellDev = Math.Abs((double)(w5m.AvgSellPrice.Value - w6h.AvgSellPrice.Value)
                               / w6h.AvgSellPrice.Value * 100.0);
        }

        return Math.Max(buyDev, sellDev);
    }
}
