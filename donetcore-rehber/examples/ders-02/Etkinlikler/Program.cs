using Etkinlikler.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<EtkinlikListesi>();

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Etkinlikler}/{action=Index}/{id?}");

app.Run();
