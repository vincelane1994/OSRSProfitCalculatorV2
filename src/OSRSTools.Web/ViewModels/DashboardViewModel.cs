using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class DashboardViewModel
{
    public string? LastSyncTime { get; set; }

    /// <summary>Top 5 High Alchemy items sorted by ROI for the dashboard carousel.</summary>
    public List<HighAlchItem> TopHighAlchItems { get; set; } = new();

    /// <summary>Top 5 flip candidates sorted by GP/hr for the dashboard carousel.</summary>
    public List<FlipCandidate> TopFlipItems { get; set; } = new();

    /// <summary>Top 5 smithing items sorted by profit per bar for the dashboard carousel.</summary>
    public List<SmithingItem> TopSmithingItems { get; set; } = new();

    /// <summary>Top 5 herblore items sorted by profit per operation for the dashboard carousel.</summary>
    public List<HerbloreItem> TopHerbloreItems { get; set; } = new();
}
