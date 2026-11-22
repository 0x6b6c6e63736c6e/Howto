using Etkinlikler.Data;
using Microsoft.AspNetCore.Authorization;
using Etkinlikler.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Controllers;

public class EtkinliklerController(EtkinlikDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var etkinlikler = await db.Etkinlikler
            .AsNoTracking()
            .OrderBy(etkinlik => etkinlik.Id)
            .ToListAsync(cancellationToken);

        return View(etkinlikler);
    }

    [Authorize(Roles = "Yonetici")]
    [HttpGet]
    public async Task<IActionResult> Doluluk(CancellationToken cancellationToken)
    {
        var rapor = await DolulukRaporu.Sorgu(db).ToListAsync(cancellationToken);
        return View(rapor);
    }

    [Authorize(Roles = "Yonetici")]
    [HttpGet]
    public IActionResult Ekle() => View(new EtkinlikFormu());

    [Authorize(Roles = "Yonetici")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(EtkinlikFormu form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        db.Etkinlikler.Add(new Etkinlik
        {
            Ad = form.Ad!,
            Aciklama = form.Aciklama,
            Kontenjan = form.Kontenjan!.Value
        });

        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
