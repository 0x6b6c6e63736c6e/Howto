using System.ComponentModel.DataAnnotations;

namespace Etkinlikler.Models;

public sealed class GirisFormu
{
    [Required(ErrorMessage = "E-posta girin.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Eposta { get; set; }

    [Required(ErrorMessage = "Parola girin.")]
    [DataType(DataType.Password)]
    public string? Parola { get; set; }

    public string? ReturnUrl { get; set; }
}
