using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Service that calculates smithing profitability for cannonballs and dart tips.
/// Coordinates data fetching and profit calculation to produce ranked lists
/// of smithing operations sorted by profit per bar.
/// </summary>
public interface ISmithingService
{
    /// <summary>
    /// Calculates cannonball smithing profitability.
    /// Cannonballs are produced from steel bars only (4 cannonballs per bar).
    /// Returns items sorted by ProfitPerUnit descending, excluding zero-volume outputs.
    /// </summary>
    Task<IEnumerable<SmithingItem>> GetCannonballProfitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates dart tip smithing profitability for all 6 bar types.
    /// All dart tip smithing is members-only and requires The Tourist Trap quest (10 tips per bar).
    /// Returns items sorted by ProfitPerUnit descending, excluding zero-volume outputs.
    /// </summary>
    Task<IEnumerable<SmithingItem>> GetDartTipProfitsAsync(CancellationToken cancellationToken = default);
}
