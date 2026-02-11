namespace OSRSTools.Core.Entities;

/// <summary>
/// Price and volume data for a specific time window (5m, 1h, 6h, 24h).
///
/// API terminology (counterintuitive):
/// - "high" price = instant-buy price (buyer overpaid to fill immediately)
/// - "low" price  = instant-sell price (seller undercut to fill immediately)
///
/// We use clear naming: AvgBuyPrice = avg instant-buy, AvgSellPrice = avg instant-sell.
/// </summary>
public class TimeWindowPrice
{
    /// <summary>Average instant-buy price for the window (API "high" avg).</summary>
    public int? AvgBuyPrice { get; init; }

    /// <summary>Average instant-sell price for the window (API "low" avg).</summary>
    public int? AvgSellPrice { get; init; }

    /// <summary>Number of instant-buys in the window.</summary>
    public int? BuyVolume { get; init; }

    /// <summary>Number of instant-sells in the window.</summary>
    public int? SellVolume { get; init; }

    /// <summary>Whether this window has any usable price data.</summary>
    public bool HasData => AvgBuyPrice.HasValue || AvgSellPrice.HasValue;

    /// <summary>Total volume (buy + sell) for this window.</summary>
    public int TotalVolume => (BuyVolume ?? 0) + (SellVolume ?? 0);
}
