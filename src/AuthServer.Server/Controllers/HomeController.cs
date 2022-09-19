using System.Diagnostics;
using AuthServer.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Server.Controllers;
public class HomeController : Controller
{
    public HomeController()
    {
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
