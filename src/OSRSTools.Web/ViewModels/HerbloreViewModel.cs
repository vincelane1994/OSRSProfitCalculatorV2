using OSRSTools.Core.Entities;

namespace OSRSTools.Web.ViewModels;

public class HerbloreViewModel
{
    public List<HerbloreItem> CleaningItems { get; set; } = new();
    public List<HerbloreItem> FullProcessItems { get; set; } = new();
    public List<HerbloreItem> PotionMakingItems { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public int TotalCleaning => CleaningItems.Count;
    public int TotalFullProcess => FullProcessItems.Count;
    public int TotalPotionMaking => PotionMakingItems.Count;
    public int ProfitableCleaning => CleaningItems.Count(x => x.IsProfitable);
    public int ProfitableFullProcess => FullProcessItems.Count(x => x.IsProfitable);
    public int ProfitablePotionMaking => PotionMakingItems.Count(x => x.IsProfitable);
}
