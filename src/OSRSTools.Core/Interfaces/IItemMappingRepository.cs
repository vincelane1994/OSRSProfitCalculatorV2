using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Repository for fetching OSRS item mapping data (static reference data).
/// </summary>
public interface IItemMappingRepository
{
    /// <summary>
    /// Fetches all item mappings from the data source.
    /// </summary>
    Task<IReadOnlyDictionary<int, ItemMapping>> GetAllMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single item mapping by ID, or null if not found.
    /// </summary>
    Task<ItemMapping?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default);
}
