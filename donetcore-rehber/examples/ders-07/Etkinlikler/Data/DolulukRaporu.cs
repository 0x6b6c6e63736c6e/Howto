using Etkinlikler.Models;

namespace Etkinlikler.Data;

public static class DolulukRaporu
{
    public static IQueryable<EtkinlikDolulugu> Sorgu(EtkinlikDbContext db)
    {
        var sayim =
            from etkinlik in db.Etkinlikler
            join kayit in db.EtkinlikKayitlari on etkinlik.Id equals kayit.EtkinlikId into kayitlar
            from kayit in kayitlar.DefaultIfEmpty()
            group (int?)kayit.KatilimciId by new { etkinlik.Id, etkinlik.Ad, etkinlik.Kontenjan } into etkinlikGrubu
            select new
            {
                etkinlikGrubu.Key.Id,
                etkinlikGrubu.Key.Ad,
                etkinlikGrubu.Key.Kontenjan,
                KayitSayisi = etkinlikGrubu.Count(katilimciId => katilimciId != null)
            };

        return sayim.OrderBy(satir => satir.Id)
            .Select(satir => new EtkinlikDolulugu(
                satir.Ad,
                satir.Kontenjan,
                satir.KayitSayisi,
                satir.Kontenjan - satir.KayitSayisi,
                Math.Round(100m * satir.KayitSayisi / satir.Kontenjan, 2)));
    }
}
