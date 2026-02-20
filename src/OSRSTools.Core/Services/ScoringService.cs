using Microsoft.Extensions.Options;
using OSRSTools.Core.Configuration;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;

namespace OSRSTools.Core.Services;

/// <summary>
/// Scores flip candidates using configurable breakpoint interpolation.
/// </summary>
public class ScoringService : IScoringService
{
    private readonly ScoringConfiguration _config;

    public ScoringService(IOptions<ScoringConfiguration> config)
    {
        _config = config.Value;
    }

    public double ScoreVolume(int volume24Hr) =>
        Interpolate(volume24Hr, _config.VolumeBreakpoints);

    public double ScoreMargin(int margin) =>
        Interpolate(margin, _config.MarginBreakpoints);

    public double ScoreRoi(double roiPercent) =>
        Interpolate(roiPercent, _config.RoiBreakpoints);

    public double CalculateConfidence(int windowsUsed, int volume24Hr)
    {
        var windowScore = Math.Min(windowsUsed / (double)_config.MinWindowsForHighConfidence, 1.0) * 0.6;
        var volumeScore = Math.Min(volume24Hr / (double)_config.MinVolumeForHighConfidence, 1.0) * 0.4;

        return Math.Round(Math.Min(windowScore + volumeScore, 1.0), 2);
    }

    public double CalculateFlipScore(FlipCandidate candidate)
    {
        var volumeScore = ScoreVolume(candidate.Volume24Hr);
        var marginScore = ScoreMargin(candidate.Margin);
        var roiScore = ScoreRoi(candidate.RoiPercent);
        var gpHrScore = candidate.GpPerHour > 0
            ? Math.Min(candidate.GpPerHour / 1_000_000.0, 1.0)
            : 0;

        var rawScore = (volumeScore * _config.VolumeWeight)
            + (marginScore * _config.MarginWeight)
            + (roiScore * _config.RoiWeight)
            + (gpHrScore * _config.GpPerHourWeight);

        var confidence = CalculateConfidence(
            candidate.HasSufficientData ? 4 : 2,
            candidate.Volume24Hr);

        return Math.Round(rawScore * confidence * 10.0, 1);
    }

    private static double Interpolate(double value, List<BreakpointEntry> breakpoints)
    {
        if (breakpoints == null || breakpoints.Count == 0)
            return 0.5;

        var sorted = breakpoints.OrderBy(b => b.Threshold).ToList();

        if (value <= sorted[0].Threshold)
            return sorted[0].Score;

        if (value >= sorted[^1].Threshold)
            return sorted[^1].Score;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (value >= sorted[i].Threshold && value <= sorted[i + 1].Threshold)
            {
                var range = sorted[i + 1].Threshold - sorted[i].Threshold;
                var progress = (value - sorted[i].Threshold) / range;
                return sorted[i].Score + progress * (sorted[i + 1].Score - sorted[i].Score);
            }
        }

        return sorted[^1].Score;
    }
}
