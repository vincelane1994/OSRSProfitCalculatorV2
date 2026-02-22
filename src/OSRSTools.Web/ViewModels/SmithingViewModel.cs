using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class SmithingViewModel
{
    public List<SmithingItem> Cannonballs { get; set; } = new();
    public List<SmithingItem> DartTips { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalCannonballs => Cannonballs.Count;
    public int TotalDartTips => DartTips.Count;
    public int ProfitableCannonballs => Cannonballs.Count(x => x.IsProfitable);
    public int ProfitableDartTips => DartTips.Count(x => x.IsProfitable);
}
