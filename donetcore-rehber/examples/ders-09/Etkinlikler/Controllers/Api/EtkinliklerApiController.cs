using Etkinlikler.Data;
using Etkinlikler.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Controllers.Api;

[ApiController]
[Route("api/etkinlikler")]
public class EtkinliklerApiController(EtkinlikDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Etkinlik>>> Listele(CancellationToken cancellationToken)
    {
        var etkinlikler = await db.Etkinlikler.AsNoTracking()
            .OrderBy(e => e.Id).ToListAsync(cancellationToken);
        return Ok(etkinlikler);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Etkinlik>> Getir(int id, CancellationToken cancellationToken)
    {
        var etkinlik = await db.Etkinlikler.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return etkinlik is null ? NotFound() : Ok(etkinlik);
    }

    [Authorize(Roles = "Yonetici")]
    [HttpGet("form-belirteci")]
    public IActionResult FormBelirteci([FromServices] IAntiforgery antiforgery)
    {
        var token = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = token.RequestToken });
    }

    [Authorize(Roles = "Yonetici")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult<Etkinlik>> Ekle(
        [FromBody] EtkinlikFormu form, CancellationToken cancellationToken)
    {
        var etkinlik = new Etkinlik { Ad = form.Ad!, Kontenjan = form.Kontenjan!.Value };
        db.Etkinlikler.Add(etkinlik);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Getir), new { id = etkinlik.Id }, etkinlik);
    }
}
