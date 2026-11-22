using Etkinlikler.Data;
using Etkinlikler.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Controllers.Api;

[ApiController]
[Route("api/etkinlikler")]
public class EtkinliklerApiController(EtkinlikDbContext db, ILogger<EtkinliklerApiController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] EtkinlikSorgusu istek, CancellationToken cancellationToken)
    {
        var sorgu = db.Etkinlikler.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(istek.Arama))
        {
            var arama = istek.Arama.Trim();
            sorgu = sorgu.Where(e => e.Ad.Contains(arama));
        }
        if (istek.EnAzKontenjan.HasValue)
            sorgu = sorgu.Where(e => e.Kontenjan >= istek.EnAzKontenjan.Value);

        var toplam = await sorgu.CountAsync(cancellationToken);
        var azalan = istek.Yon == "azalan";
        sorgu = istek.Sirala switch
        {
            "ad" when azalan => sorgu.OrderByDescending(e => e.Ad).ThenBy(e => e.Id),
            "ad" => sorgu.OrderBy(e => e.Ad).ThenBy(e => e.Id),
            "kontenjan" when azalan => sorgu.OrderByDescending(e => e.Kontenjan).ThenBy(e => e.Id),
            "kontenjan" => sorgu.OrderBy(e => e.Kontenjan).ThenBy(e => e.Id),
            _ when azalan => sorgu.OrderByDescending(e => e.Id),
            _ => sorgu.OrderBy(e => e.Id)
        };

        var kayitlar = await sorgu.Skip((istek.Sayfa - 1) * istek.SayfaBoyutu)
            .Take(istek.SayfaBoyutu).ToListAsync(cancellationToken);
        return Ok(new { istek.Sayfa, istek.SayfaBoyutu, toplamKayit = toplam, kayitlar });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Etkinlik>> Getir(int id, CancellationToken cancellationToken)
    {
        var etkinlik = await db.Etkinlikler.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (etkinlik is null)
        {
            logger.LogWarning("Etkinlik bulunamadı. EtkinlikId: {EtkinlikId}, İstekKimliği: {IstekKimligi}", id, HttpContext.TraceIdentifier);
            return NotFound();
        }
        return Ok(etkinlik);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Yonetici")]
    [HttpPost]
    public async Task<ActionResult<Etkinlik>> Ekle(
        [FromBody] EtkinlikFormu form, CancellationToken cancellationToken)
    {
        var etkinlik = new Etkinlik { Ad = form.Ad!, Kontenjan = form.Kontenjan!.Value };
        db.Etkinlikler.Add(etkinlik);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Etkinlik oluşturuldu. EtkinlikId: {EtkinlikId}", etkinlik.Id);
        return CreatedAtAction(nameof(Getir), new { id = etkinlik.Id }, etkinlik);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Yonetici")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Guncelle(
        int id, [FromBody] EtkinlikFormu form, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var kilitlenen = await db.Etkinlikler.Where(e => e.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(e => e.Kontenjan, e => e.Kontenjan), cancellationToken);
        if (kilitlenen == 0)
            return NotFound();

        var kayitSayisi = await db.EtkinlikKayitlari.CountAsync(k => k.EtkinlikId == id, cancellationToken);
        if (form.Kontenjan!.Value < kayitSayisi)
        {
            logger.LogWarning("Kontenjan güncellemesi reddedildi. EtkinlikId: {EtkinlikId}, KayitSayisi: {KayitSayisi}", id, kayitSayisi);
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Kontenjan yetersiz",
                detail: "Kontenjan mevcut kayıt sayısından az olamaz.",
                extensions: new Dictionary<string, object?> { ["kayitSayisi"] = kayitSayisi, ["istekKimligi"] = HttpContext.TraceIdentifier });
        }

        var etkinlik = await db.Etkinlikler.SingleAsync(e => e.Id == id, cancellationToken);
        etkinlik.Ad = form.Ad!;
        etkinlik.Kontenjan = form.Kontenjan.Value;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Etkinlik güncellendi. EtkinlikId: {EtkinlikId}", id);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Yonetici")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var kilitlenen = await db.Etkinlikler.Where(e => e.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(e => e.Kontenjan, e => e.Kontenjan), cancellationToken);
        if (kilitlenen == 0)
            return NotFound();

        var etkinlik = await db.Etkinlikler.SingleAsync(e => e.Id == id, cancellationToken);
        db.Etkinlikler.Remove(etkinlik);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Etkinlik silindi. EtkinlikId: {EtkinlikId}", id);
        return NoContent();
    }
}
