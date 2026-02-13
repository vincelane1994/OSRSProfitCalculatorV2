using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHighAlchingService _highAlchingService;

    public HomeController(IHighAlchingService highAlchingService)
    {
        _highAlchingService = highAlchingService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        try
        {
            var items = await _highAlchingService.GetProfitableItemsAsync();
            model.TopHighAlchItems = items
                .OrderByDescending(x => x.RoiPercent)
                .Take(5)
                .ToList();
        }
        catch
        {
            // Dashboard should still render even if High Alch data fails
        }

        return View(model);
    }
}
