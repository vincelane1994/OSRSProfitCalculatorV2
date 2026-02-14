using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Pure domain service for profit-related calculations.
/// Contains no I/O — all data is passed in as parameters.
/// </summary>
public interface IProfitCalculationService
{
    /// <summary>
    /// Calculates GE tax for a sell price using configured rate and cap.
    /// </summary>
    TaxCalculation CalculateTax(int sellPrice);

    /// <summary>
    /// Calculates profit for a generic buy→sell operation (1 input → 1 output).
    /// Used for High Alching (buy item → alch for gold).
    /// </summary>
    ProfitCalculation CalculateSimpleProfit(int buyPrice, int sellPrice, int maxQuantity);

    /// <summary>
    /// Calculates profit for a multi-output operation (1 input → N outputs).
    /// Used for Cannonballs (1 bar → 4 cannonballs) and Dart Tips (1 bar → 10 tips).
    /// </summary>
    ProfitCalculation CalculateMultiOutputProfit(int inputPrice, int outputPrice, int outputPerInput, int maxQuantity);

    /// <summary>
    /// Calculates the maximum quantity affordable given a buy price and investment cap.
    /// </summary>
    int CalculateMaxQuantity(int buyPrice, long maxInvestment, int buyLimit);

    /// <summary>
    /// Calculates estimated fill time in hours for a two-leg flip (buy + sell).
    /// </summary>
    double CalculateEstimatedFillHours(int buyLimit, int quantity, int volume24Hr, double buyLimitCycleHours);

    /// <summary>
    /// Calculates GP/hr: totalProfit / estimatedFillHours.
    /// </summary>
    double CalculateGpPerHour(long totalProfit, double estimatedFillHours);
}
