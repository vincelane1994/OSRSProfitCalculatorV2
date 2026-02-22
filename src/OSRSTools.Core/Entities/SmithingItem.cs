namespace OSRSTools.Core.Entities;

/// <summary>
/// Represents a single smithing operation evaluated for profitability.
/// Profit = (OutputPrice * OutputPerInput) - BarPrice.
/// </summary>
public class SmithingItem
{
    /// <summary>OSRS item ID of the output item (e.g., 2 for Cannonball, 819 for Bronze dart tip).</summary>
    public int ItemId { get; init; }

    /// <summary>Display name of the output item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Type of smithing operation.</summary>
    public SmithingType Type { get; init; }

    /// <summary>Whether the smithing operation requires a members account.</summary>
    public bool Members { get; init; }

    /// <summary>Display name of the bar used as input (e.g., "Steel bar").</summary>
    public string BarName { get; init; } = string.Empty;

    /// <summary>OSRS item ID of the input bar.</summary>
    public int BarId { get; init; }

    /// <summary>Recommended GE buy price for one input bar.</summary>
    public int BarPrice { get; init; }

    /// <summary>Recommended GE sell price for one output item.</summary>
    public int OutputPrice { get; init; }

    /// <summary>Number of output items produced per input bar (4 for cannonballs, 10 for dart tips).</summary>
    public int OutputPerInput { get; init; }

    /// <summary>Profit per bar smelted: (OutputPrice * OutputPerInput) - BarPrice.</summary>
    public int ProfitPerUnit { get; init; }

    /// <summary>Total profit based on available 24h volume.</summary>
    public long TotalProfit { get; init; }

    /// <summary>24-hour trading volume of the output item.</summary>
    public int Volume24Hr { get; init; }

    /// <summary>Return on investment percentage: (ProfitPerUnit / BarPrice) * 100.</summary>
    public double RoiPercent { get; init; }

    /// <summary>Whether this smithing operation is profitable.</summary>
    public bool IsProfitable => ProfitPerUnit > 0;
}
