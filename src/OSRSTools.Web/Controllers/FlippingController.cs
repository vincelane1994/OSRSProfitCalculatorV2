using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Entities;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class FlippingController : Controller
{
    private readonly IFlipAnalyzer _flipAnalyzer;

    public FlippingController(IFlipAnalyzer flipAnalyzer)
    {
        _flipAnalyzer = flipAnalyzer;
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
            var viewModel = new FlippingViewModel
            {
                ErrorMessage = "Failed to load Flipping data. Please try again later."
            };

            ViewBag.Error = ex.Message;
            return View(viewModel);
        }
    }
}
