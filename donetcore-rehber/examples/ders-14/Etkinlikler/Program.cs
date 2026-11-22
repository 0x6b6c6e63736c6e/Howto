using Etkinlikler.Data;
using Etkinlikler.Hatalar;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["istekKimligi"] = context.HttpContext.TraceIdentifier);
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddScoped<EtkinlikKayitServisi>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<EtkinlikDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ayarını girin.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key en az 32 bayt olmalı.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer ayarını girin."),
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience ayarını girin."),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

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

app.UseExceptionHandler();
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
{
    api.UseStatusCodePages();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Etkinlikler}/{action=Index}/{id?}");

await app.RunAsync();

public partial class Program { }
