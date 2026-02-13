using OSRSTools.Core.Entities;
using OSRSTools.Core.ValueObjects;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Calculates complete flip profitability for a single item.
/// Takes item info, recommended prices, and user settings; returns a fully populated FlipCandidate.
/// </summary>
public interface IFlipCalculator
{
    /// <summary>
    /// Calculates margin, tax, quantity, fill time, ROI, GP/hr and returns a FlipCandidate.
    /// ConfidenceRating and FlipScore are left at 0 — set later by IScoringService.
    /// </summary>
    FlipCandidate CalculateFlip(
        int itemId,
        string name,
        bool members,
        int buyLimit,
        PriceRecommendation prices,
        int volume24Hr,
        FlipSettings settings);
}
