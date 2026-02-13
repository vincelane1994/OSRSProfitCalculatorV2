using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class DashboardViewModel
{
    public string? LastSyncTime { get; set; }

    /// <summary>Top 5 High Alchemy items sorted by ROI for the dashboard carousel.</summary>
    public List<HighAlchItem> TopHighAlchItems { get; set; } = new();

    public double? TopFlippingGpPerHour { get; set; }
    public string? TopFlippingItem { get; set; }
    public int? TopSmithingProfit { get; set; }
    public string? TopSmithingItem { get; set; }
    public int? TopHerbloreProfit { get; set; }
    public string? TopHerbloreItem { get; set; }
}
