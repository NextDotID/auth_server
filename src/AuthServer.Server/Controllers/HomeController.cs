using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Server.Controllers;
public class HomeController : Controller
{
    public HomeController()
    {
    }

    public IActionResult Index()
    {
        return View("Error", "Dashboard not implemented yet");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("Error", "Unknown");
    }
}
