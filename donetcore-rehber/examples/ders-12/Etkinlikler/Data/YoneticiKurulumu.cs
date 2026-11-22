using Microsoft.AspNetCore.Identity;

namespace Etkinlikler.Data;

public static class YoneticiKurulumu
{
    public static async Task CalistirAsync(IServiceProvider services, IConfiguration config)
    {
        var email = config["IlkYonetici:Eposta"] ?? throw new InvalidOperationException("İlk yönetici e-postasını ayarlayın.");
        var parola = config["IlkYonetici:Parola"] ?? throw new InvalidOperationException("İlk yönetici parolasını ayarlayın.");
        await using var scope = services.CreateAsyncScope();
        var roller = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var kullanicilar = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roller.RoleExistsAsync("Yonetici"))
        {
            var rolSonucu = await roller.CreateAsync(new IdentityRole("Yonetici"));
            if (!rolSonucu.Succeeded) throw new InvalidOperationException(string.Join(", ", rolSonucu.Errors.Select(e => e.Description)));
        }

        var kullanici = await kullanicilar.FindByEmailAsync(email);
        if (kullanici is null)
        {
            kullanici = new IdentityUser { UserName = email, Email = email };
            var sonuc = await kullanicilar.CreateAsync(kullanici, parola);
            if (!sonuc.Succeeded) throw new InvalidOperationException(string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        }

        if (!await kullanicilar.IsInRoleAsync(kullanici, "Yonetici"))
        {
            var sonuc = await kullanicilar.AddToRoleAsync(kullanici, "Yonetici");
            if (!sonuc.Succeeded) throw new InvalidOperationException(string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        }
        Console.WriteLine("Yönetici hesabı ve rolü hazır.");
    }
}
