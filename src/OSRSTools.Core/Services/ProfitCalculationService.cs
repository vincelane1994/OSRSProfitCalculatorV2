using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
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

    public ProfitCalculationService(IOptions<TaxSettings> taxSettings)
    {
        _taxSettings = taxSettings.Value;
    }

    public TaxCalculation CalculateTax(int sellPrice)
    {
        return TaxCalculation.Calculate(sellPrice, _taxSettings.Rate, _taxSettings.Cap);
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

        var buyHours = (double)buyLimit / hourlyVolume;
        var sellHours = (double)quantity / hourlyVolume;
        var rawFillHours = buyHours + sellHours;

        // The buy limit resets every buyLimitCycleHours (default 4h).
        // Even if offers fill faster, we cannot restart the buy side until the
        // cycle resets — so effective throughput is capped by one cycle per 4h.
        return Math.Max(rawFillHours, buyLimitCycleHours);
    }

    public double CalculateGpPerHour(long totalProfit, double estimatedFillHours)
    {
        if (estimatedFillHours <= 0) return 0;
        return totalProfit / estimatedFillHours;
    }

}
