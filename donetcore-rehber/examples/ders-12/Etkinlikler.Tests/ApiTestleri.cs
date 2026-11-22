using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Etkinlikler.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Etkinlikler.Tests;

public class ApiTestleri
{
    private const string Key = "test-only-signing-key-32-bytes-minimum-123456";

    [Fact]
    public async Task Kimlik_ve_rol_kontrolleri()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-api" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EtkinlikDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<EtkinlikDbContext>>();
                services.AddDbContext<EtkinlikDbContext>(options => options.UseSqlite(connection));
            });
        });
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtkinlikDbContext>();
            await db.Database.EnsureCreatedAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("Yonetici"))).Succeeded);
            var admin = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com" };
            var uye = new IdentityUser { UserName = "uye@example.com", Email = "uye@example.com" };
            Assert.True((await users.CreateAsync(admin, "Deneme!123")).Succeeded);
            Assert.True((await users.CreateAsync(uye, "Deneme!123")).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, "Yonetici")).Succeeded);
        }

        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        async Task<HttpStatusCode> PostAsync() =>
            (await client.PostAsJsonAsync("/api/etkinlikler", new { ad = "JWT Atölyesi", kontenjan = 30 })).StatusCode;
        Assert.Equal(HttpStatusCode.Unauthorized, await PostAsync());
        using var yanlis = await client.PostAsJsonAsync("/api/token", new { eposta = "admin@example.com", parola = "Yanlis!123" });
        Assert.Equal(HttpStatusCode.Unauthorized, yanlis.StatusCode);

        async Task<string> TokenAsync(string email)
        {
            using var response = await client.PostAsJsonAsync("/api/token", new { eposta = email, parola = "Deneme!123" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
            return (await response.Content.ReadFromJsonAsync<TokenCevabi>())!.AccessToken;
        }
        var uyeToken = await TokenAsync("uye@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", uyeToken);
        Assert.Equal(HttpStatusCode.Forbidden, await PostAsync());

        var adminToken = await TokenAsync("admin@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.Created, await PostAsync());

        string TokenUret(string issuer, string audience, string key, DateTime expires)
        {
            var jwt = new JwtSecurityToken(issuer, audience, [new Claim(ClaimTypes.Role, "Yonetici")],
                expires: expires, signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        foreach (var token in new[]
        {
            TokenUret("yanlis", "test-api", Key, DateTime.UtcNow.AddMinutes(5)),
            TokenUret("test-issuer", "yanlis", Key, DateTime.UtcNow.AddMinutes(5)),
            TokenUret("test-issuer", "test-api", "another-test-key-32-bytes-minimum-12345678", DateTime.UtcNow.AddMinutes(5)),
            TokenUret("test-issuer", "test-api", Key, DateTime.UtcNow.AddMinutes(-5))
        })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Unauthorized, await PostAsync());
        }

        client.DefaultRequestHeaders.Authorization = null;
        using var giris = await client.GetAsync("/Hesap/Giris");
        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);
    }

    private sealed record TokenCevabi(string AccessToken);
}
