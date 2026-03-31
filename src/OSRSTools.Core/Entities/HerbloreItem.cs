namespace OSRSTools.Core.Entities;

/// <summary>
/// Represents a single herblore operation evaluated for profitability.
/// One row per herb per method (e.g., Ranarr cleaning, Ranarr full process, Ranarr potion making).
/// </summary>
public class HerbloreItem
{
    /// <summary>OSRS item ID of the output item (clean herb, unfinished potion, or finished potion).</summary>
    public int ItemId { get; init; }

    /// <summary>Display name of the output item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Type of herblore operation.</summary>
    public HerbloreMethod Method { get; init; }

    /// <summary>Display name of the herb (e.g., "Ranarr weed").</summary>
    public string HerbName { get; init; } = string.Empty;

    /// <summary>
    /// Recommended GE buy price for the primary input herb.
    /// Grimy herb for Cleaning and FullProcess; clean herb for PotionMaking.
    /// </summary>
    public int HerbPrice { get; init; }

    /// <summary>
    /// Recommended GE buy price for the secondary input ingredient, or null for Cleaning.
    /// Vial of water for FullProcess; secondary ingredient for PotionMaking.
    /// </summary>
    public int? SecondaryPrice { get; init; }

    /// <summary>Recommended GE sell price for the output item.</summary>
    public int OutputPrice { get; init; }

    /// <summary>Profit per operation: OutputPrice - HerbPrice - (SecondaryPrice ?? 0).</summary>
    public int ProfitPerUnit { get; init; }

    /// <summary>24-hour trading volume of the output item.</summary>
    public int Volume24Hr { get; init; }

    /// <summary>Return on investment percentage: (ProfitPerUnit / TotalInputCost) * 100.</summary>
    public double RoiPercent { get; init; }

    /// <summary>All herblore operations are members-only.</summary>
    public bool Members { get; init; }

    /// <summary>URL to the item's icon image.</summary>
    public string? IconUrl { get; init; }

    /// <summary>Whether this operation yields a positive profit.</summary>
    public bool IsProfitable => ProfitPerUnit > 0;
}
