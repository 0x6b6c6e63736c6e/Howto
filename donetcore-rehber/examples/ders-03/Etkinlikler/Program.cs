using Etkinlikler.Data;
using Microsoft.EntityFrameworkCore;

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
