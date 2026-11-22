using System.Data;
using Etkinlikler.Models;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Data;

public enum KayitSonucu
{
    Basarili,
    EtkinlikBulunamadi,
    KatilimciBulunamadi,
    ZatenKayitli,
    KontenjanDolu
}

public sealed class EtkinlikKayitServisi(EtkinlikDbContext db)
{
    public async Task<KayitSonucu> KaydetAsync(
        int etkinlikId, int katilimciId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        var etkinlik = await db.Etkinlikler
            .FromSql($"""
                SELECT Id, Ad, Kontenjan
                FROM dbo.Etkinlikler WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = {etkinlikId}
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (etkinlik is null)
        {
            return KayitSonucu.EtkinlikBulunamadi;
        }

        if (!await db.Set<Katilimci>().AnyAsync(k => k.Id == katilimciId, cancellationToken))
        {
            return KayitSonucu.KatilimciBulunamadi;
        }

        var kayitlar = db.EtkinlikKayitlari.Where(k => k.EtkinlikId == etkinlikId);
        if (await kayitlar.AnyAsync(k => k.KatilimciId == katilimciId, cancellationToken))
        {
            return KayitSonucu.ZatenKayitli;
        }

        if (await kayitlar.CountAsync(cancellationToken) >= etkinlik.Kontenjan)
        {
            return KayitSonucu.KontenjanDolu;
        }

        db.EtkinlikKayitlari.Add(new EtkinlikKaydi
        {
            EtkinlikId = etkinlikId,
            KatilimciId = katilimciId
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return KayitSonucu.Basarili;
    }
}
