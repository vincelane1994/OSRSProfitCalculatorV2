using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Scores flip candidates using breakpoint interpolation and confidence ratings.
/// </summary>
public interface IScoringService
{
    /// <summary>Scores volume using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreVolume(int volume24Hr);

    /// <summary>Scores total profit using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreTotalProfit(long totalProfit);

    /// <summary>Scores ROI using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreRoi(double roiPercent);

    /// <summary>
    /// Calculates confidence rating based on data quality, volume, and price stability.
    /// Higher with more time windows, higher volume, and lower volatility. Returns 0.0–1.0.
    /// </summary>
    double CalculateConfidence(int windowsUsed, int volume24Hr, double volatilityPercent);

    /// <summary>
    /// Calculates composite flip score from sub-scores, weighted and adjusted by confidence.
    /// Returns 0.0–10.0 scale.
    /// </summary>
    double CalculateFlipScore(FlipCandidate candidate);
}
