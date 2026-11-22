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
builder.Services.AddScoped<EtkinlikKayitServisi>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<EtkinlikDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Hesap/Giris";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
});

var connection = builder.Configuration.GetConnectionString("EtkinlikDb")
    ?? throw new InvalidOperationException("ConnectionStrings:EtkinlikDb ayarını girin.");

builder.Services.AddDbContext<EtkinlikDbContext>(options =>
    options.UseSqlServer(connection));

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Etkinlikler}/{action=Index}/{id?}");

await app.RunAsync();
