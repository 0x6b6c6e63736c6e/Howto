using Etkinlikler.Data;
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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var connection = builder.Configuration.GetConnectionString("EtkinlikDb")
    ?? throw new InvalidOperationException("ConnectionStrings:EtkinlikDb ayarını girin.");

builder.Services.AddDbContext<EtkinlikDbContext>(options =>
    options.UseSqlServer(connection));

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Etkinlikler}/{action=Index}/{id?}");

app.Run();
