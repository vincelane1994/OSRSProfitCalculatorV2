using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHighAlchingService _highAlchingService;
    private readonly IFlipAnalyzer _flipAnalyzer;
    private readonly ISmithingService _smithingService;
    private readonly IHerbloreService _herbloreService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHighAlchingService highAlchingService,
        IFlipAnalyzer flipAnalyzer,
        ISmithingService smithingService,
        IHerbloreService herbloreService,
        ILogger<HomeController> logger)
    {
        _highAlchingService = highAlchingService;
        _flipAnalyzer = flipAnalyzer;
        _smithingService = smithingService;
        _herbloreService = herbloreService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        var alchTask = FetchHighAlchAsync(model);
        var flipTask = FetchFlipsAsync(model);
        var smithTask = FetchSmithingAsync(model);
        var herbTask = FetchHerbloreAsync(model);

        await Task.WhenAll(alchTask, flipTask, smithTask, herbTask);

        model.LastSyncTime = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

        return View(model);
    }

    private async Task FetchHighAlchAsync(DashboardViewModel model)
    {
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
    }

    private async Task FetchFlipsAsync(DashboardViewModel model)
    {
        try
        {
            var items = await _flipAnalyzer.AnalyzeFlipsAsync(new FlipSettings());
            model.TopFlipItems = items
                .OrderByDescending(x => x.GpPerHour)
                .Take(5)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Flipping data for dashboard");
        }
    }

    private async Task FetchSmithingAsync(DashboardViewModel model)
    {
        try
        {
            var cannonTask = _smithingService.GetCannonballProfitsAsync();
            var dartTask = _smithingService.GetDartTipProfitsAsync();
            await Task.WhenAll(cannonTask, dartTask);

            model.TopSmithingItems = (await cannonTask)
                .Concat(await dartTask)
                .OrderByDescending(x => x.ProfitPerUnit)
                .Take(5)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Smithing data for dashboard");
        }
    }

    private async Task FetchHerbloreAsync(DashboardViewModel model)
    {
        try
        {
            var cleanTask = _herbloreService.GetCleaningProfitsAsync();
            var fullTask = _herbloreService.GetFullProcessProfitsAsync();
            var potionTask = _herbloreService.GetPotionMakingProfitsAsync();
            await Task.WhenAll(cleanTask, fullTask, potionTask);

            model.TopHerbloreItems = (await cleanTask)
                .Concat(await fullTask)
                .Concat(await potionTask)
                .OrderByDescending(x => x.ProfitPerUnit)
                .Take(5)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Herblore data for dashboard");
        }
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
