using Etkinlikler.Data;
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

    [HttpGet]
    public async Task<IActionResult> Doluluk(CancellationToken cancellationToken)
    {
        var rapor = await DolulukRaporu.Sorgu(db).ToListAsync(cancellationToken);
        return View(rapor);
    }

    [HttpGet]
    public IActionResult Ekle() => View(new EtkinlikFormu());

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
            Kontenjan = form.Kontenjan!.Value
        });

        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
