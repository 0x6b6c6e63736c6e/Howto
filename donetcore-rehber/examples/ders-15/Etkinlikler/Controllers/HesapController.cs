using Etkinlikler.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Etkinlikler.Controllers;

public class HesapController(SignInManager<IdentityUser> giris) : Controller
{
    [HttpGet]
    public IActionResult Giris(string? returnUrl) => View(new GirisFormu { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisFormu form)
    {
        if (!ModelState.IsValid) return View(form);

        var sonuc = await giris.PasswordSignInAsync(form.Eposta!, form.Parola!, false, lockoutOnFailure: true);
        if (sonuc.Succeeded)
        {
            return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
        }

        ModelState.AddModelError(string.Empty, sonuc.IsLockedOut
            ? "Hesap geçici olarak kilitlendi."
            : "E-posta veya parola geçersiz.");
        return View(form);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cikis()
    {
        await giris.SignOutAsync();
        return RedirectToAction("Index", "Etkinlikler");
    }

    [HttpGet]
    public IActionResult ErisimEngellendi() => View();
}
