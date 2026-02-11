namespace OSRSTools.Core.Entities;

/// <summary>
/// Aggregated price data for a single item across all time windows.
///
/// API terminology (counterintuitive):
/// - "high" price = instant-buy price (buyer overpaid to fill immediately)
/// - "low" price  = instant-sell price (seller undercut to fill immediately)
///
/// For a patient flip (place limit orders and wait):
/// - Recommended buy price derives from instant-sell ("low") data
/// - Recommended sell price derives from instant-buy ("high") data
/// </summary>
public class ItemPriceData
{
    public int ItemId { get; init; }

    /// <summary>Latest instant-buy price (API "high" from /latest).</summary>
    public int? LatestBuyPrice { get; init; }

    /// <summary>Latest instant-sell price (API "low" from /latest).</summary>
    public int? LatestSellPrice { get; init; }

    /// <summary>Timestamp of the latest instant-buy.</summary>
    public DateTime? LatestBuyTime { get; init; }

    /// <summary>Timestamp of the latest instant-sell.</summary>
    public DateTime? LatestSellTime { get; init; }

    /// <summary>
    /// Time window price data keyed by window type.
    /// Contains entries for 5m, 1h, 6h, and 24h windows when available.
    /// </summary>
    public Dictionary<TimeWindow, TimeWindowPrice> TimeWindows { get; init; } = new();

    /// <summary>Returns the number of time windows that have usable price data.</summary>
    public int AvailableWindowCount => TimeWindows.Count(tw => tw.Value.HasData);

    /// <summary>24-hour total volume (buy + sell) if available, otherwise 0.</summary>
    public int Volume24Hr =>
        TimeWindows.TryGetValue(TimeWindow.TwentyFourHour, out var window)
            ? window.TotalVolume
            : 0;
}
