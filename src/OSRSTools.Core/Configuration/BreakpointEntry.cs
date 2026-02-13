namespace OSRSTools.Core.Configuration;

/// <summary>
/// A single (threshold, score) pair used for breakpoint interpolation.
/// </summary>
public class BreakpointEntry
{
    /// <summary>The input value threshold.</summary>
    public double Threshold { get; set; }

    /// <summary>The score assigned at this threshold (0.0 to 1.0).</summary>
    public double Score { get; set; }
}
