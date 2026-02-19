using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHighAlchingService _highAlchingService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHighAlchingService highAlchingService,
        ILogger<HomeController> logger)
    {
        _highAlchingService = highAlchingService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load High Alchemy data for dashboard");
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
