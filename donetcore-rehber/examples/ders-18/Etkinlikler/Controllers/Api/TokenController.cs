using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Etkinlikler.Controllers.Api;

public sealed class TokenIstegi
{
    [Required, EmailAddress]
    public string? Eposta { get; set; }

    [Required]
    public string? Parola { get; set; }
}

[ApiController]
[Route("api/token")]
public class TokenController(
    IWebHostEnvironment environment,
    IConfiguration config,
    UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signIn) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Olustur(TokenIstegi istek)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var user = await users.FindByEmailAsync(istek.Eposta!);
        if (user is null)
            return Unauthorized();

        var sonuc = await signIn.CheckPasswordSignInAsync(user, istek.Parola!, lockoutOnFailure: true);
        if (!sonuc.Succeeded)
            return Unauthorized();

        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ayarını girin.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key en az 32 bayt olmalı.");

        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var expires = DateTime.UtcNow.AddMinutes(15);
        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer ayarını girin."),
            audience: config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience ayarını girin."),
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(jwt), expiresAtUtc = expires });
    }
}
