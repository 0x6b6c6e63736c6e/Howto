using Etkinlikler.Models;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Data;

public static class KayitDeneyi
{
    public static async Task CalistirAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var kontrolScope = services.CreateAsyncScope();
        var db = kontrolScope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
        var katilimcilar = await db.Set<Katilimci>().OrderBy(k => k.Id).Select(k => k.Id)
            .Take(2).ToListAsync(cancellationToken);
        if (katilimcilar.Count != 2)
        {
            throw new InvalidOperationException("Deney için en az iki katılımcı gerekir.");
        }

        var etkinlik = new Etkinlik { Ad = "Son yer deneyi", Kontenjan = 1 };
        db.Etkinlikler.Add(etkinlik);
        await db.SaveChangesAsync(cancellationToken);

        async Task<KayitSonucu> DeneAsync(int katilimciId)
        {
            await using var scope = services.CreateAsyncScope();
            var kayit = scope.ServiceProvider.GetRequiredService<EtkinlikKayitServisi>();
            return await kayit.KaydetAsync(etkinlik.Id, katilimciId, cancellationToken);
        }

        var sonuclar = await Task.WhenAll(DeneAsync(katilimcilar[0]), DeneAsync(katilimcilar[1]));
        var kayitSayisi = await db.EtkinlikKayitlari.CountAsync(
            k => k.EtkinlikId == etkinlik.Id, cancellationToken);

        Console.WriteLine($"Etkinlik #{etkinlik.Id}: {string.Join(", ", sonuclar)}. Toplam kayıt: {kayitSayisi}.");
        if (kayitSayisi != 1 || !sonuclar.Contains(KayitSonucu.Basarili)
            || !sonuclar.Contains(KayitSonucu.KontenjanDolu))
        {
            throw new InvalidOperationException("Deney beklenen sonucu vermedi.");
        }
    }
}
