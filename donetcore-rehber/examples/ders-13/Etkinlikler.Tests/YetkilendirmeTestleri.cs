using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Etkinlikler.Tests;

public class YetkilendirmeTestleri
{
    private static WebApplicationFactory<Program> Uygulama() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EtkinlikDb"] = "Server=localhost;Database=Test;Integrated Security=True;Encrypt=True"
                })));

    [Theory]
    [InlineData("/Etkinlikler/Ekle")]
    [InlineData("/Etkinlikler/Doluluk")]
    [InlineData("/Kayitlar/Ekle")]
    public async Task Anonim_kullanici_girise_yonlendirilir(string adres)
    {
        await using var app = Uygulama();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(adres);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Hesap/Giris", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Giris_formu_anonim_erisimde_acilir()
    {
        await using var app = Uygulama();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/Hesap/Giris");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("__RequestVerificationToken", await response.Content.ReadAsStringAsync());
    }
}
