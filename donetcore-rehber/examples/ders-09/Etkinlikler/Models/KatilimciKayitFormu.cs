using System.ComponentModel.DataAnnotations;

namespace Etkinlikler.Models;

public sealed class KatilimciKayitFormu
{
    [Display(Name = "Etkinlik")]
    [Required(ErrorMessage = "Etkinlik seçin.")]
    public int? EtkinlikId { get; set; }

    [Display(Name = "Katılımcı")]
    [Required(ErrorMessage = "Katılımcı seçin.")]
    public int? KatilimciId { get; set; }
}
