namespace OSRSTools.Core.ValueObjects;

/// <summary>
/// Immutable result of a profit calculation for any calculator type.
/// Used as a common output for High Alch, Smithing, Herblore, and Flipping results.
/// </summary>
public readonly record struct ProfitCalculation
{
    /// <summary>Profit per single operation (per cast, per bar, per herb, per flip).</summary>
    public int ProfitPerUnit { get; init; }

    /// <summary>Total cost to perform the operation once.</summary>
    public int CostPerUnit { get; init; }

    /// <summary>Revenue from a single operation.</summary>
    public int RevenuePerUnit { get; init; }

    /// <summary>Maximum quantity achievable given investment constraints.</summary>
    public int Quantity { get; init; }

    /// <summary>Total investment required (costPerUnit * quantity).</summary>
    public long TotalInvestment { get; init; }

    /// <summary>Total profit (profitPerUnit * quantity).</summary>
    public long TotalProfit { get; init; }

    /// <summary>Return on investment percentage: (profit / cost) * 100.</summary>
    public double RoiPercent { get; init; }

    /// <summary>Whether this calculation represents a profitable operation.</summary>
    public bool IsProfitable => ProfitPerUnit > 0;
}
