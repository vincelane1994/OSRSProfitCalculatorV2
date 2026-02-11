using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Repository for fetching OSRS item price data across all time windows.
/// </summary>
public interface IPriceRepository
{
    /// <summary>
    /// Fetches latest instant prices for all items (/latest endpoint).
    /// Returns a dictionary keyed by item ID.
    /// </summary>
    Task<IReadOnlyDictionary<int, ItemPriceData>> GetLatestPricesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches average prices for a specific time window (/5m, /1h, /6h, /24h).
    /// Returns a dictionary of TimeWindowPrice keyed by item ID.
    /// </summary>
    Task<IReadOnlyDictionary<int, TimeWindowPrice>> GetTimeWindowPricesAsync(
        TimeWindow window,
        CancellationToken cancellationToken = default);
}
