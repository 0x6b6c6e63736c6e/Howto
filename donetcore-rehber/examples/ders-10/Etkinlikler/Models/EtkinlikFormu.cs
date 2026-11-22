using System.ComponentModel.DataAnnotations;

namespace Etkinlikler.Models;

public sealed class EtkinlikFormu
{
    [Display(Name = "Etkinlik adı")]
    [Required(ErrorMessage = "Etkinlik adını girin.")]
    [StringLength(80, MinimumLength = 3,
        ErrorMessage = "Etkinlik adı 3 ile 80 karakter arasında olmalı.")]
    public string? Ad { get; set; }

    [Required(ErrorMessage = "Kontenjanı girin.")]
    [Range(1, 500, ErrorMessage = "Kontenjan 1 ile 500 arasında olmalı.")]
    public int? Kontenjan { get; set; }
}
