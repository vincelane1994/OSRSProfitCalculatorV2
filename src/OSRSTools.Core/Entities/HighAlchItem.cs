namespace OSRSTools.Core.Entities;

/// <summary>
/// Represents a single item evaluated for High Level Alchemy profitability.
/// Profit = HighAlchValue - BuyPrice - NatureRuneCost.
/// </summary>
public class HighAlchItem
{
    /// <summary>OSRS item ID.</summary>
    public int ItemId { get; init; }

    /// <summary>Display name of the item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether the item is members-only.</summary>
    public bool Members { get; init; }

    /// <summary>GE buy limit per 4-hour cycle.</summary>
    public int BuyLimit { get; init; }

    /// <summary>Recommended GE buy price based on weighted time window data.</summary>
    public int BuyPrice { get; init; }

    /// <summary>Gold received from casting High Level Alchemy on this item.</summary>
    public int HighAlchValue { get; init; }

    /// <summary>Cost of one Nature Rune (required reagent per cast).</summary>
    public int NatureRuneCost { get; init; }

    /// <summary>Profit per cast: HighAlchValue - BuyPrice - NatureRuneCost.</summary>
    public int Profit { get; init; }

    /// <summary>24-hour trading volume for this item.</summary>
    public int Volume24Hr { get; init; }

    /// <summary>Return on investment percentage.</summary>
    public double RoiPercent { get; init; }

    /// <summary>Whether this item is profitable to alch.</summary>
    public bool IsProfitable => Profit > 0;
}
