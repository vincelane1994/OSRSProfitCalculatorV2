using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HighAlchingController : Controller
{
    private readonly IHighAlchingService _highAlchingService;

    public HighAlchingController(IHighAlchingService highAlchingService)
    {
        _highAlchingService = highAlchingService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var items = await _highAlchingService.GetProfitableItemsAsync();

            ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

            var viewModel = new HighAlchViewModel
            {
                Items = items.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            var viewModel = new HighAlchViewModel
            {
                ErrorMessage = "Failed to load High Alchemy data. Please try again later."
            };

            ViewBag.Error = ex.Message;
            return View(viewModel);
        }
    }
}
