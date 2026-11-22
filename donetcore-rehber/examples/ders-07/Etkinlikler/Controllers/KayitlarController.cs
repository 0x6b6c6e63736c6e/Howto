using Etkinlikler.Data;
using Microsoft.AspNetCore.Authorization;
using Etkinlikler.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Controllers;

[Authorize(Roles = "Yonetici")]
public class KayitlarController(EtkinlikDbContext db, EtkinlikKayitServisi kayitServisi) : Controller
{
    [HttpGet]
    public Task<IActionResult> Ekle(CancellationToken cancellationToken)
        => FormuGosterAsync(new KatilimciKayitFormu(), cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(KatilimciKayitFormu form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormuGosterAsync(form, cancellationToken);
        }

        var sonuc = await kayitServisi.KaydetAsync(
            form.EtkinlikId!.Value, form.KatilimciId!.Value, cancellationToken);

        if (sonuc == KayitSonucu.Basarili)
        {
            TempData["KayitMesaji"] = "Katılımcı kaydedildi.";
            return RedirectToAction("Doluluk", "Etkinlikler");
        }

        var mesaj = sonuc switch
        {
            KayitSonucu.EtkinlikBulunamadi => "Etkinlik bulunamadı.",
            KayitSonucu.KatilimciBulunamadi => "Katılımcı bulunamadı.",
            KayitSonucu.ZatenKayitli => "Bu katılımcı etkinliğe zaten kayıtlı.",
            _ => "Etkinliğin kontenjanı dolu."
        };

        ModelState.AddModelError(string.Empty, mesaj);
        return await FormuGosterAsync(form, cancellationToken);
    }

    private async Task<IActionResult> FormuGosterAsync(
        KatilimciKayitFormu form, CancellationToken cancellationToken)
    {
        var etkinlikler = await db.Etkinlikler.AsNoTracking().OrderBy(e => e.Ad).ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
        var katilimcilar = await db.Set<Katilimci>().AsNoTracking().OrderBy(k => k.Ad).ThenBy(k => k.Id)
            .ToListAsync(cancellationToken);

        ViewBag.Etkinlikler = new SelectList(etkinlikler, nameof(Etkinlik.Id), nameof(Etkinlik.Ad));
        ViewBag.Katilimcilar = new SelectList(katilimcilar, nameof(Katilimci.Id), nameof(Katilimci.Ad));
        return View(nameof(Ekle), form);
    }
}
