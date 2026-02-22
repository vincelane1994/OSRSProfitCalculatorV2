using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class SmithingController : Controller
{
    private readonly ISmithingService _smithingService;

    public SmithingController(ISmithingService smithingService)
    {
        _smithingService = smithingService;
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
            var viewModel = new SmithingViewModel
            {
                ErrorMessage = "Failed to load Smithing data. Please try again later."
            };

            ViewBag.Error = ex.Message;
            return View(viewModel);
        }
    }
}
