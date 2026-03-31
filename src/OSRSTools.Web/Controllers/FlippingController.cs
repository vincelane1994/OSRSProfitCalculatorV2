using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class FlippingController : Controller
{
    private readonly IFlipAnalyzer _flipAnalyzer;
    private readonly ILogger<FlippingController> _logger;

    public FlippingController(IFlipAnalyzer flipAnalyzer, ILogger<FlippingController> logger)
    {
        _flipAnalyzer = flipAnalyzer;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var settings = new FlipSettings();
            var items = await _flipAnalyzer.AnalyzeFlipsAsync(settings);

            ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

            var viewModel = new FlippingViewModel
            {
                Items = items.ToList(),
                CurrentSettings = settings
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Flipping data");
            var errorMessage = ex is HttpRequestException
                ? "Unable to reach the OSRS pricing service. Please try again in a moment."
                : "Failed to load Flipping data. Please try again later.";
            return View(new FlippingViewModel { ErrorMessage = errorMessage });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Data()
    {
        try
        {
            var settings = new FlipSettings();
            var items = await _flipAnalyzer.AnalyzeFlipsAsync(settings);
            return Json(items, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Flipping data");
            return StatusCode(500);
        }
    }
}
