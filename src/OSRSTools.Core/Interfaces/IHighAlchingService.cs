using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Service that evaluates all tradeable items for High Level Alchemy profitability.
/// Coordinates data fetching and profit calculation to produce a ranked list
/// of profitable items to alch.
/// </summary>
public interface IHighAlchingService
{
    /// <summary>
    /// Fetches all item data and calculates High Alchemy profitability for each item.
    /// Returns items sorted by profit descending, filtered to only include
    /// items that have a valid high alch value, buy price, and non-zero volume.
    /// </summary>
    Task<IReadOnlyList<HighAlchItem>> GetProfitableItemsAsync(CancellationToken cancellationToken = default);
}
