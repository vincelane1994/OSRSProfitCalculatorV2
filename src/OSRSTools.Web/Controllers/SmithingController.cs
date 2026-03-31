using System.Text.Json;
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

    [HttpGet]
    public async Task<IActionResult> Data()
    {
        try
        {
            var cannonballsTask = _smithingService.GetCannonballProfitsAsync();
            var dartTipsTask = _smithingService.GetDartTipProfitsAsync();
            await Task.WhenAll(cannonballsTask, dartTipsTask);
            return Json(new { cannonballs = await cannonballsTask, dartTips = await dartTipsTask },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Smithing data");
            return StatusCode(500);
        }
    }
}
