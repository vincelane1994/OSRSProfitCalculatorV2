using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HerbloreController : Controller
{
    private readonly IHerbloreService _herbloreService;
    private readonly ILogger<HerbloreController> _logger;

    public HerbloreController(IHerbloreService herbloreService, ILogger<HerbloreController> logger)
    {
        _herbloreService = herbloreService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var cleaningTask = _herbloreService.GetCleaningProfitsAsync();
            var fullProcessTask = _herbloreService.GetFullProcessProfitsAsync();
            var potionMakingTask = _herbloreService.GetPotionMakingProfitsAsync();
            await Task.WhenAll(cleaningTask, fullProcessTask, potionMakingTask);

            ViewData["LastSync"] = DateTime.UtcNow.ToString("h:mm tt") + " UTC";

            var viewModel = new HerbloreViewModel
            {
                CleaningItems = (await cleaningTask).ToList(),
                FullProcessItems = (await fullProcessTask).ToList(),
                PotionMakingItems = (await potionMakingTask).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Herblore data");
            var errorMessage = ex is HttpRequestException
                ? "Unable to reach the OSRS pricing service. Please try again in a moment."
                : "Failed to load Herblore data. Please try again later.";
            return View(new HerbloreViewModel { ErrorMessage = errorMessage });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Data()
    {
        try
        {
            var cleaningTask = _herbloreService.GetCleaningProfitsAsync();
            var fullProcessTask = _herbloreService.GetFullProcessProfitsAsync();
            var potionMakingTask = _herbloreService.GetPotionMakingProfitsAsync();
            await Task.WhenAll(cleaningTask, fullProcessTask, potionMakingTask);
            return Json(new
            {
                cleaningItems = await cleaningTask,
                fullProcessItems = await fullProcessTask,
                potionMakingItems = await potionMakingTask
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Herblore data");
            return StatusCode(500);
        }
    }
}
