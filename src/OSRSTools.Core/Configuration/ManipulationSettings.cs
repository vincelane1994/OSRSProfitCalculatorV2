namespace OSRSTools.Core.Configuration;

/// <summary>
/// Configuration for market manipulation detection thresholds.
/// Bound via IOptions&lt;ManipulationSettings&gt; from appsettings.json.
/// </summary>
public class ManipulationSettings
{
    /// <summary>Max allowed % deviation between 5m and 24h prices before flagging.</summary>
    public double PriceDeviationThresholdPercent { get; set; } = 25.0;

    /// <summary>Max allowed buy/sell volume ratio before flagging.</summary>
    public double VolumeRatioThreshold { get; set; } = 10.0;

    /// <summary>ROI threshold above which low-volume items are suspicious.</summary>
    public double HighRoiThresholdPercent { get; set; } = 8.0;

    /// <summary>Volume threshold below which high-ROI items are suspicious.</summary>
    public int LowVolumeThreshold { get; set; } = 5_000;
}
