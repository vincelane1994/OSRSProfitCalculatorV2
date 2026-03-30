namespace OSRSTools.Core.Entities;

/// <summary>
/// Represents a single item evaluated as a GE flipping candidate.
/// Profit = (RecommendedSellPrice - RecommendedBuyPrice) - TaxAmount per unit.
/// </summary>
public class FlipCandidate
{
    /// <summary>OSRS item ID.</summary>
    public int ItemId { get; init; }

    /// <summary>Display name of the item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether the item is members-only.</summary>
    public bool Members { get; init; }

    /// <summary>GE buy limit per 4-hour cycle.</summary>
    public int BuyLimit { get; init; }

    /// <summary>Recommended instant-buy price based on weighted time window data.</summary>
    public int RecommendedBuyPrice { get; init; }

    /// <summary>Recommended instant-sell price based on weighted time window data.</summary>
    public int RecommendedSellPrice { get; init; }

    /// <summary>Gross margin: RecommendedSellPrice - RecommendedBuyPrice (before tax).</summary>
    public int Margin { get; init; }

    /// <summary>GE tax amount deducted from the sell price.</summary>
    public int TaxAmount { get; init; }

    /// <summary>Net profit per unit: Margin - TaxAmount.</summary>
    public int ProfitPerUnit { get; init; }

    /// <summary>Number of items to flip (limited by buy limit and max investment).</summary>
    public int Quantity { get; init; }

    /// <summary>Total profit for the full quantity (long to avoid overflow on high-value flips).</summary>
    public long TotalProfit { get; init; }

    /// <summary>Return on investment percentage.</summary>
    public double RoiPercent { get; init; }

    /// <summary>Estimated gold earned per hour based on fill time and profit.</summary>
    public double GpPerHour { get; init; }

    /// <summary>Estimated hours to fill the full quantity based on 24-hour volume.</summary>
    public double EstimatedFillHours { get; init; }

    /// <summary>24-hour trading volume for this item.</summary>
    public int Volume24Hr { get; init; }

    /// <summary>Confidence rating from 0.0 (low) to 1.0 (high) based on data availability.</summary>
    public double ConfidenceRating { get; set; }

    /// <summary>Composite flip score combining volume, margin, ROI, and GP/hr sub-scores.</summary>
    public double FlipScore { get; set; }

    /// <summary>Whether enough time window data exists for a reliable recommendation.</summary>
    public bool HasSufficientData { get; init; }

    /// <summary>Number of time windows used to calculate the recommended buy price.</summary>
    public int BuyWindowsUsed { get; init; }

    /// <summary>Number of time windows used to calculate the recommended sell price.</summary>
    public int SellWindowsUsed { get; init; }

    /// <summary>Price volatility percentage: max deviation of 5m vs 6h avg prices (buy or sell).</summary>
    public double PriceVolatilityPercent { get; init; }

    /// <summary>Profit per 4-hour buy limit cycle: ProfitPerUnit × BuyLimit.</summary>
    public long ProfitPerCycle { get; init; }

    /// <summary>Raw time window price data for detail view.</summary>
    public List<WindowPriceSnapshot> WindowPrices { get; init; } = [];

    /// <summary>Whether this flip is profitable after tax.</summary>
    public bool IsProfitable => ProfitPerUnit > 0;
}

/// <summary>
/// Serializable snapshot of a single time window's price and volume data.
/// </summary>
public class WindowPriceSnapshot
{
    public string Window { get; init; } = string.Empty;
    public int? AvgBuyPrice { get; init; }
    public int? AvgSellPrice { get; init; }
    public int? BuyVolume { get; init; }
    public int? SellVolume { get; init; }
}
