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
        int volume24Hr,
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

        // 7. Fill time estimate
        var fillHours = _profitCalcService.CalculateEstimatedFillHours(
            buyLimit, quantity, volume24Hr, settings.BuyLimitCycleHours);

        // 8. GP/hr
        var gpPerHour = _profitCalcService.CalculateGpPerHour(totalProfit, fillHours);

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
            ConfidenceRating = 0,
            FlipScore = 0
        };
    }
}
