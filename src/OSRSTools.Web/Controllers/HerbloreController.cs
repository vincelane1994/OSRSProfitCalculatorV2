using Microsoft.AspNetCore.Mvc;
using OSRSTools.Core.Interfaces;
using OSRSTools.Web.ViewModels;

namespace OSRSTools.Web.Controllers;

public class HerbloreController : Controller
{
    private readonly IHerbloreService _herbloreService;

    public HerbloreController(IHerbloreService herbloreService)
    {
        _herbloreService = herbloreService;
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
            var viewModel = new HerbloreViewModel
            {
                ErrorMessage = "Failed to load Herblore data. Please try again later."
            };

            ViewBag.Error = ex.Message;
            return View(viewModel);
        }
    }
}
