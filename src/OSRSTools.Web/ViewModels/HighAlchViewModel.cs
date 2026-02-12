using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class HighAlchViewModel
{
    public List<HighAlchItem> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalItems => Items.Count;
    public int ProfitableItems => Items.Count(x => x.IsProfitable);
}
