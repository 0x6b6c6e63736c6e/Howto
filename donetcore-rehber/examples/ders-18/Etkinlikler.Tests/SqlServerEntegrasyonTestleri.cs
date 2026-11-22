using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Etkinlikler.Data;
using Etkinlikler.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Etkinlikler.Tests;

public class SqlServerEntegrasyonTestleri
{
    [Fact]
    public async Task Jwt_roller_dogrulama_ve_son_yer()
    {
        var connection = Environment.GetEnvironmentVariable("ETKINLIK_TEST_SQL")
            ?? throw new InvalidOperationException("ETKINLIK_TEST_SQL ile boş test veritabanının bağlantısını ayarlayın.");
        var connectionBuilder = new SqlConnectionStringBuilder(connection);
        if (string.IsNullOrWhiteSpace(connectionBuilder.InitialCatalog))
            throw new InvalidOperationException("Bağlantıda ayrı bir test veritabanı belirtilmeli.");

        // Önceden var olan tabloya veya veriye dokunulmaz.
        await using (var sql = new SqlConnection(connection))
        {
            await sql.OpenAsync();
            await using var check = sql.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 0)
                throw new InvalidOperationException("Test için tablo içermeyen boş bir veritabanı seçin.");
        }

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EtkinlikDb"] = connection,
                    ["Jwt:Key"] = key,
                    ["Jwt:Issuer"] = "entegrasyon",
                    ["Jwt:Audience"] = "etkinlik-test"
                }));
        });

        int etkinlikId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            await db.Database.MigrateAsync();
            var eventItem = new Etkinlik { Ad = "Eşzamanlı test", Kontenjan = 1 };
            db.Etkinlikler.Add(eventItem);
            await db.SaveChangesAsync();
            etkinlikId = eventItem.Id;

            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("Yonetici"))).Succeeded);
            var admin = new IdentityUser { UserName = "admin@test.local", Email = "admin@test.local" };
            var member = new IdentityUser { UserName = "uye@test.local", Email = "uye@test.local" };
            Assert.True((await users.CreateAsync(admin, "Test!12345")).Succeeded);
            Assert.True((await users.CreateAsync(member, "Test!12345")).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, "Yonetici")).Succeeded);
        }

        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        async Task<string> TokenAsync(string email)
        {
            using var response = await client.PostAsJsonAsync("/api/token", new { eposta = email, parola = "Test!12345" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<TokenCevabi>())!.AccessToken;
        }

        using (var anonymous = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "Entegrasyon", kontenjan = 10 }))
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "gecersiz-token");
        using (var invalidToken = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "Entegrasyon", kontenjan = 10 }))
            Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync("uye@test.local"));
        using (var forbidden = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "Entegrasyon", kontenjan = 10 }))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync("admin@test.local"));
        using (var invalid = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "AB", kontenjan = 0 }))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (var created = await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "Entegrasyon", kontenjan = 10 }))
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<KayitSonucu> KaydetAsync(int katilimci)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<EtkinlikKayitServisi>();
            await gate.Task.WaitAsync(timeout.Token);
            return await service.KaydetAsync(etkinlikId, katilimci, timeout.Token);
        }
        var ilk = KaydetAsync(1);
        var ikinci = KaydetAsync(2);
        gate.SetResult();
        var results = await Task.WhenAll(ilk, ikinci);
        Assert.Equal(1, results.Count(r => r == KayitSonucu.Basarili));
        Assert.Equal(1, results.Count(r => r == KayitSonucu.KontenjanDolu));
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            Assert.Equal(1, await db.EtkinlikKayitlari.CountAsync(k => k.EtkinlikId == etkinlikId));
        }
    }

    private sealed record TokenCevabi(string AccessToken);
}
