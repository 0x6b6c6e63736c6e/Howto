using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Etkinlikler.Data;

public sealed class KomutSayaci : DbCommandInterceptor
{
    public List<string> Komutlar { get; } = [];

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Komutlar.Add(command.CommandText);
        return ValueTask.FromResult(result);
    }
}

public sealed record RaporSatiri(int Id, string Etkinlik, IReadOnlyList<string> Katilimcilar);
public sealed record Olcum(string Yontem, int KomutSayisi, int IzlenenVarlik,
    IReadOnlyList<RaporSatiri> Sonuc, IReadOnlyList<string> Sql);

public static class SorguKarsilastirmasi
{
    public static async Task<IReadOnlyList<Olcum>> CalistirAsync(
        Func<KomutSayaci, EtkinlikDbContext> contextOlustur, CancellationToken cancellationToken = default)
    {
        var sonuclar = new List<Olcum>();
        foreach (var yontem in new[] { "N+1", "Include", "Include + AsNoTracking", "Projection" })
        {
            var sayac = new KomutSayaci();
            await using var db = contextOlustur(sayac);
            List<RaporSatiri> rapor;

            if (yontem == "N+1")
            {
                var etkinlikler = await db.Etkinlikler.AsNoTracking().OrderBy(e => e.Id).ToListAsync(cancellationToken);
                rapor = [];
                foreach (var etkinlik in etkinlikler)
                {
                    var adlar = await db.EtkinlikKayitlari.AsNoTracking()
                        .Where(k => k.EtkinlikId == etkinlik.Id)
                        .OrderBy(k => k.KatilimciId)
                        .Select(k => k.Katilimci.Ad).ToListAsync(cancellationToken);
                    rapor.Add(new RaporSatiri(etkinlik.Id, etkinlik.Ad, adlar));
                }
            }
            else if (yontem == "Projection")
            {
                var satirlar = await db.Etkinlikler.AsNoTracking().OrderBy(e => e.Id)
                    .Select(e => new
                    {
                        e.Id,
                        e.Ad,
                        Katilimcilar = e.Kayitlar.OrderBy(k => k.KatilimciId)
                            .Select(k => k.Katilimci.Ad).ToList()
                    }).ToListAsync(cancellationToken);
                rapor = satirlar.Select(e => new RaporSatiri(e.Id, e.Ad, e.Katilimcilar)).ToList();
            }
            else
            {
                var sorgu = db.Etkinlikler.Include(e => e.Kayitlar).ThenInclude(k => k.Katilimci)
                    .AsSingleQuery().OrderBy(e => e.Id).AsQueryable();
                if (yontem == "Include + AsNoTracking")
                    sorgu = sorgu.AsNoTracking();
                var etkinlikler = await sorgu.ToListAsync(cancellationToken);
                rapor = etkinlikler.Select(e => new RaporSatiri(e.Id, e.Ad,
                    e.Kayitlar.OrderBy(k => k.KatilimciId).Select(k => k.Katilimci.Ad).ToList())).ToList();
            }

            sonuclar.Add(new Olcum(yontem, sayac.Komutlar.Count,
                db.ChangeTracker.Entries().Count(), rapor, sayac.Komutlar.ToArray()));
        }
        return sonuclar;
    }
}
