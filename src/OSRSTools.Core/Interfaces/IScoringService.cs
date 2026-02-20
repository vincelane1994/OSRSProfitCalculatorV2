using OSRSTools.Core.Entities;

namespace OSRSTools.Core.Interfaces;

/// <summary>
/// Scores flip candidates using breakpoint interpolation and confidence ratings.
/// </summary>
public interface IScoringService
{
    /// <summary>Scores volume using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreVolume(int volume24Hr);

    /// <summary>Scores margin using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreMargin(int margin);

    /// <summary>Scores ROI using breakpoint interpolation. Returns 0.0–1.0.</summary>
    double ScoreRoi(double roiPercent);

    /// <summary>
    /// Calculates confidence rating based on data quality.
    /// Higher with more time windows and higher volume. Returns 0.0–1.0.
    /// </summary>
    double CalculateConfidence(int windowsUsed, int volume24Hr);

    /// <summary>
    /// Calculates composite flip score from sub-scores, weighted and adjusted by confidence.
    /// Returns 0.0–10.0 scale.
    /// </summary>
    double CalculateFlipScore(FlipCandidate candidate);
}
