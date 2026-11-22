using Etkinlikler.Models;
using Microsoft.AspNetCore.Mvc;

namespace Etkinlikler.Controllers;

public class EtkinliklerController(EtkinlikListesi liste) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(liste.Listele());

    [HttpGet]
    public IActionResult Ekle() => View(new EtkinlikFormu());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Ekle(EtkinlikFormu form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        liste.Ekle(new Etkinlik(form.Ad!, form.Kontenjan!.Value));
        return RedirectToAction(nameof(Index));
    }
}
