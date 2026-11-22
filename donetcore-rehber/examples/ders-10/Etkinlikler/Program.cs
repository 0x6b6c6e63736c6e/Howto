using Etkinlikler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

if (args is ["--rapor-sql"])
{
    // SQL üretmek için sağlayıcı gerekir. Bu bağlantı açılmaz.
    var options = new DbContextOptionsBuilder<EtkinlikDbContext>()
        .UseSqlServer("Server=localhost;Database=DotNetAtolyeEtkinlikler;Integrated Security=True;Encrypt=True")
        .Options;
    using var db = new EtkinlikDbContext(options);
    Console.WriteLine(DolulukRaporu.Sorgu(db).ToQueryString());
    return;
}

var yoneticiOlustur = args is ["--yonetici-olustur"];
var kayitDeneyi = args is ["--kayit-deneyi"];
var builder = WebApplication.CreateBuilder(kayitDeneyi || yoneticiOlustur ? [] : args);
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddScoped<EtkinlikKayitServisi>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<EtkinlikDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Hesap/Giris";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
});

builder.Services.AddDbContext<EtkinlikDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connection = configuration.GetConnectionString("EtkinlikDb")
        ?? throw new InvalidOperationException("ConnectionStrings:EtkinlikDb ayarını girin.");
    options.UseSqlServer(connection);
});

await using var app = builder.Build();

if (yoneticiOlustur)
{
    await YoneticiKurulumu.CalistirAsync(app.Services, builder.Configuration);
    return;
}

if (kayitDeneyi)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await KayitDeneyi.CalistirAsync(app.Services, timeout.Token);
    return;
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Etkinlikler}/{action=Index}/{id?}");

await app.RunAsync();

public partial class Program { }
