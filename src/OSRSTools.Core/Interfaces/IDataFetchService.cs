using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Orchestrator service that coordinates fetching item mappings, prices,
/// and volume data from all API endpoints. Combines the results into
/// fully-populated ItemPriceData objects with all available time windows.
/// </summary>
public interface IDataFetchService
{
    /// <summary>
    /// Fetches all item mappings (cached).
    /// </summary>
    Task<IReadOnlyDictionary<int, ItemMapping>> GetMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches and assembles complete price data for all items,
    /// including latest prices and all time window averages.
    /// </summary>
    Task<IReadOnlyDictionary<int, ItemPriceData>> GetCompletePriceDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the high alchemy value for an item by ID, or null if unavailable.
    /// </summary>
    Task<int?> GetHighAlchValueAsync(int itemId, CancellationToken cancellationToken = default);
}
