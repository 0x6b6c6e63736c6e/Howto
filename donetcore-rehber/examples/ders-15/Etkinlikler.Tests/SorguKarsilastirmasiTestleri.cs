using Etkinlikler.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Etkinlikler.Tests;

public class SorguKarsilastirmasiTestleri(ITestOutputHelper output)
{
    [Fact]
    public async Task Yontemler_ayni_sonucu_ve_beklenen_komut_sayilarini_verir()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EtkinlikDbContext>().UseSqlite(connection).Options;
        await using (var db = new EtkinlikDbContext(options))
            await db.Database.EnsureCreatedAsync();

        var olcumler = await SorguKarsilastirmasi.CalistirAsync(sayac =>
            new EtkinlikDbContext(new DbContextOptionsBuilder<EtkinlikDbContext>()
                .UseSqlite(connection).AddInterceptors(sayac).Options));

        foreach (var olcum in olcumler)
        {
            output.WriteLine($"{olcum.Yontem}: {olcum.KomutSayisi} komut, {olcum.IzlenenVarlik} izlenen");
            foreach (var sql in olcum.Sql) output.WriteLine(sql);
        }

        Assert.Equal(new[] { 3, 1, 1, 1 }, olcumler.Select(o => o.KomutSayisi));
        Assert.True(olcumler[1].IzlenenVarlik > 0);
        Assert.Equal(0, olcumler[2].IzlenenVarlik);
        Assert.Equal(0, olcumler[3].IzlenenVarlik);
        foreach (var sonuc in olcumler)
        {
            Assert.Equal(2, sonuc.Sonuc.Count);
            Assert.Equal(new[] { "Deniz Kaya", "Ece Arslan", "Mert Yılmaz" }, sonuc.Sonuc[0].Katilimcilar);
            Assert.Empty(sonuc.Sonuc[1].Katilimcilar);
            Assert.Equal(sonuc.KomutSayisi, sonuc.Sql.Count);
        }
    }
}
