using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Service that calculates herblore profitability across three operation types
/// for all 14 tradeable herbs.
/// </summary>
public interface IHerbloreService
{
    /// <summary>
    /// Calculates profit from cleaning grimy herbs into clean herbs.
    /// Profit = cleanSellPrice - grimyBuyPrice.
    /// Returns items sorted by ProfitPerUnit descending, excluding zero-volume outputs.
    /// </summary>
    Task<IReadOnlyList<HerbloreItem>> GetCleaningProfitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates profit from combining a grimy herb with a vial of water to make an unfinished potion.
    /// Profit = unfSellPrice - grimyBuyPrice - vialBuyPrice.
    /// Returns items sorted by ProfitPerUnit descending, excluding zero-volume outputs.
    /// </summary>
    Task<IReadOnlyList<HerbloreItem>> GetFullProcessProfitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates profit from combining a clean herb with a secondary ingredient to make a finished potion.
    /// Profit = potionSellPrice - cleanBuyPrice - secondaryBuyPrice.
    /// Returns items sorted by ProfitPerUnit descending, excluding zero-volume outputs.
    /// </summary>
    Task<IReadOnlyList<HerbloreItem>> GetPotionMakingProfitsAsync(CancellationToken cancellationToken = default);
}
