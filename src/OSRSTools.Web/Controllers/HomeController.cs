using Microsoft.AspNetCore.Mvc;

namespace OSRSTools.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
