namespace OSRSTools.Core.Configuration;

/// <summary>
/// Configuration for the scoring system that ranks flip candidates.
/// Bound via IOptions&lt;ScoringConfiguration&gt; from appsettings.json.
/// </summary>
public class ScoringConfiguration
{
    /// <summary>Breakpoints for scoring items by 24-hour volume.</summary>
    public List<BreakpointEntry> VolumeBreakpoints { get; set; } = [];

    /// <summary>Breakpoints for scoring items by total profit (budget-adjusted).</summary>
    public List<BreakpointEntry> TotalProfitBreakpoints { get; set; } = [];

    /// <summary>Breakpoints for scoring items by ROI percentage.</summary>
    public List<BreakpointEntry> RoiBreakpoints { get; set; } = [];

    /// <summary>Weight applied to the volume sub-score.</summary>
    public double VolumeWeight { get; set; } = 0.20;

    /// <summary>Weight applied to the total-profit sub-score.</summary>
    public double TotalProfitWeight { get; set; } = 0.35;

    /// <summary>Weight applied to the ROI sub-score.</summary>
    public double RoiWeight { get; set; } = 0.20;

    /// <summary>Weight applied to the GP/hr sub-score.</summary>
    public double GpPerHourWeight { get; set; } = 0.25;

    /// <summary>Minimum number of time windows with data for high confidence rating.</summary>
    public int MinWindowsForHighConfidence { get; set; } = 3;

    /// <summary>Minimum 24-hour volume for high confidence rating.</summary>
    public int MinVolumeForHighConfidence { get; set; } = 50_000;
}
