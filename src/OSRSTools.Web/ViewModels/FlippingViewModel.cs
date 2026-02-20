using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class FlippingViewModel
{
    public List<FlipCandidate> Items { get; set; } = new();
    public FlipSettings CurrentSettings { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalItems => Items.Count;
    public int ProfitableItems => Items.Count(x => x.IsProfitable);
    public double AverageConfidence => Items.Count > 0
        ? Math.Round(Items.Average(x => x.ConfidenceRating), 2)
        : 0;
}
