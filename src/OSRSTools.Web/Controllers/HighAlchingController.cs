using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HighAlchingController : Controller
{
    private readonly IHighAlchingService _highAlchingService;
    private readonly ILogger<HighAlchingController> _logger;

    public HighAlchingController(IHighAlchingService highAlchingService, ILogger<HighAlchingController> logger)
    {
        _highAlchingService = highAlchingService;
        _logger = logger;
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
            _logger.LogError(ex, "Failed to load High Alchemy data");
            var errorMessage = ex is HttpRequestException
                ? "Unable to reach the OSRS pricing service. Please try again in a moment."
                : "Failed to load High Alchemy data. Please try again later.";
            return View(new HighAlchViewModel { ErrorMessage = errorMessage });
        }
    }
}
