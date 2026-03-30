using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class SmithingController : Controller
{
    private readonly ISmithingService _smithingService;
    private readonly ILogger<SmithingController> _logger;

    public SmithingController(ISmithingService smithingService, ILogger<SmithingController> logger)
    {
        _smithingService = smithingService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var cannonballsTask = _smithingService.GetCannonballProfitsAsync();
            var dartTipsTask = _smithingService.GetDartTipProfitsAsync();
            await Task.WhenAll(cannonballsTask, dartTipsTask);

            ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

            var viewModel = new SmithingViewModel
            {
                Cannonballs = (await cannonballsTask).ToList(),
                DartTips = (await dartTipsTask).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Smithing data");
            var errorMessage = ex is HttpRequestException
                ? "Unable to reach the OSRS pricing service. Please try again in a moment."
                : "Failed to load Smithing data. Please try again later.";
            return View(new SmithingViewModel { ErrorMessage = errorMessage });
        }
    }
}
