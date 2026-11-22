using Microsoft.AspNetCore.Mvc;

namespace Etkinlikler.Controllers;

public class EtkinliklerController : Controller
{
    public IActionResult Index()
    {
        string[] etkinlikler = ["Web Geliştirme Atölyesi", "SQL Server Semineri"];
        return View(etkinlikler);
    }
}
