using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Etkinlikler.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Etkinlikler.Tests;

public class ApiTestleri
{
    [Fact]
    public async Task Listeleme_ve_yonetici_ekleme_akisi()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EtkinlikDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<EtkinlikDbContext>>();
                services.AddDbContext<EtkinlikDbContext>(options => options.UseSqlite(connection));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestKimligi>("Test", _ => { });
            });
        });
        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>().Database.EnsureCreatedAsync();
        }

        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            db.Etkinlikler.Add(new Etkinlikler.Models.Etkinlik { Ad = "SQL Server Semineri", Kontenjan = 40 });
            await db.SaveChangesAsync();
        }
        var esitSirali = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler?arama=SQL&sirala=kontenjan&yon=azalan&sayfaBoyutu=1&sayfa=2");
        Assert.Equal(2, esitSirali!.ToplamKayit);
        Assert.True(esitSirali.Kayitlar[0].Id > 2);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            var gecici = await db.Etkinlikler.SingleAsync(e => e.Id == esitSirali.Kayitlar[0].Id);
            db.Etkinlikler.Remove(gecici);
            await db.SaveChangesAsync();
        }

        var liste = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler");
        Assert.Equal(2, liste!.ToplamKayit);
        Assert.Equal(2, liste.Kayitlar.Count);
        var ikinci = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler?sayfa=2&sayfaBoyutu=1");
        Assert.Equal(2, ikinci!.ToplamKayit);
        Assert.Single(ikinci.Kayitlar);
        Assert.Equal(2, ikinci.Kayitlar[0].Id);
        var filtre = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler?arama=SQL&enAzKontenjan=30");
        Assert.Single(filtre!.Kayitlar);
        Assert.Equal(2, filtre.Kayitlar[0].Id);
        var sirali = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler?sirala=kontenjan&yon=azalan");
        Assert.Equal(new[] { 2, 1 }, sirali!.Kayitlar.Select(e => e.Id));
        var bos = await client.GetFromJsonAsync<SayfaCevabi>("/api/etkinlikler?sayfa=10");
        Assert.Empty(bos!.Kayitlar);
        Assert.Equal(2, bos.ToplamKayit);
        foreach (var adres in new[] { "?sayfa=0", "?sayfa=1000001", "?sayfaBoyutu=101", "?sayfa=abc", "?sirala=uygunsuz", "?yon=yanlis", "?enAzKontenjan=0" })
        {
            using var gecersiz = await client.GetAsync("/api/etkinlikler" + adres);
            Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);
        }

        using var anonim = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "API Atölyesi", kontenjan = 30 });
        Assert.Equal(HttpStatusCode.Unauthorized, anonim.StatusCode);

        client.DefaultRequestHeaders.Add("X-Test-Role", "Uye");
        using var yetkisiz = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "API Atölyesi", kontenjan = 30 });
        Assert.Equal(HttpStatusCode.Forbidden, yetkisiz.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Yonetici");

        using var belirtecsiz = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "API Atölyesi", kontenjan = 30 });
        Assert.Equal(HttpStatusCode.BadRequest, belirtecsiz.StatusCode);

        var token = await client.GetFromJsonAsync<TokenCevabi>("/api/etkinlikler/form-belirteci");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token!.Token);
        using var hatali = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "AB", kontenjan = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, hatali.StatusCode);

        using var response = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "API Atölyesi", kontenjan = 30 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var olusan = await response.Content.ReadFromJsonAsync<EtkinlikCevabi>();
        Assert.Equal("API Atölyesi", olusan!.Ad);
        Assert.NotNull(response.Headers.Location);
        var getirilen = await client.GetFromJsonAsync<EtkinlikCevabi>(response.Headers.Location);
        Assert.Equal(olusan, getirilen);

        using var guncelle = await client.PutAsJsonAsync($"/api/etkinlikler/{olusan.Id}", new { ad = "Yeni API Atölyesi", kontenjan = 50 });
        Assert.Equal(HttpStatusCode.NoContent, guncelle.StatusCode);
        var guncellenen = await client.GetFromJsonAsync<EtkinlikCevabi>($"/api/etkinlikler/{olusan.Id}");
        Assert.Equal("Yeni API Atölyesi", guncellenen!.Ad);
        Assert.Equal(50, guncellenen.Kontenjan);

        using var aralikHatasi = await client.PutAsJsonAsync($"/api/etkinlikler/{olusan.Id}", new { ad = "AB", kontenjan = 501 });
        Assert.Equal(HttpStatusCode.BadRequest, aralikHatasi.StatusCode);
        using var dolulukHatasi = await client.PutAsJsonAsync("/api/etkinlikler/1", new { ad = "Web Geliştirme Atölyesi", kontenjan = 2 });
        Assert.Equal(HttpStatusCode.Conflict, dolulukHatasi.StatusCode);
        var korunmus = await client.GetFromJsonAsync<EtkinlikCevabi>("/api/etkinlikler/1");
        Assert.Equal(25, korunmus!.Kontenjan);

        using var bulunamadi = await client.PutAsJsonAsync("/api/etkinlikler/999999", new { ad = "Deneme", kontenjan = 10 });
        Assert.Equal(HttpStatusCode.NotFound, bulunamadi.StatusCode);
        using var sil = await client.DeleteAsync($"/api/etkinlikler/{olusan.Id}");
        Assert.Equal(HttpStatusCode.NoContent, sil.StatusCode);
        using var silinen = await client.GetAsync($"/api/etkinlikler/{olusan.Id}");
        Assert.Equal(HttpStatusCode.NotFound, silinen.StatusCode);
        using var tekrarSil = await client.DeleteAsync($"/api/etkinlikler/{olusan.Id}");
        Assert.Equal(HttpStatusCode.NotFound, tekrarSil.StatusCode);

        using var iliskiliSil = await client.DeleteAsync("/api/etkinlikler/1");
        Assert.Equal(HttpStatusCode.NoContent, iliskiliSil.StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            Assert.False(await db.EtkinlikKayitlari.AnyAsync(k => k.EtkinlikId == 1));
            Assert.Equal(3, await db.Set<Etkinlikler.Models.Katilimci>().CountAsync());
        }

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var csrf = await client.DeleteAsync("/api/etkinlikler/2");
        Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        using var anonimSil = await client.DeleteAsync("/api/etkinlikler/2");
        Assert.Equal(HttpStatusCode.Unauthorized, anonimSil.StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Uye");
        using var uyePut = await client.PutAsJsonAsync("/api/etkinlikler/2", new { ad = "Yeni Ad", kontenjan = 10 });
        Assert.Equal(HttpStatusCode.Forbidden, uyePut.StatusCode);
    }

    private sealed record SayfaCevabi(int Sayfa, int SayfaBoyutu, int ToplamKayit, List<EtkinlikCevabi> Kayitlar);
    private sealed record TokenCevabi(string Token);
    private sealed record EtkinlikCevabi(int Id, string Ad, int Kontenjan);
}

public sealed class TestKimligi(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[] { new Claim(ClaimTypes.Name, "test"), new Claim(ClaimTypes.Role, role.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
