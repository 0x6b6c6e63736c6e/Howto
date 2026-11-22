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
        var liste = await client.GetFromJsonAsync<List<EtkinlikCevabi>>("/api/etkinlikler");
        Assert.Equal(2, liste!.Count);

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
    }

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
