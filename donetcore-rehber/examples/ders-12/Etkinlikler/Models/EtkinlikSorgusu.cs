using System.ComponentModel.DataAnnotations;

namespace Etkinlikler.Models;

public sealed class EtkinlikSorgusu
{
    [Range(1, 1_000_000)]
    public int Sayfa { get; set; } = 1;

    [Range(1, 100)]
    public int SayfaBoyutu { get; set; } = 10;

    [StringLength(80)]
    public string? Arama { get; set; }

    [Range(1, 500)]
    public int? EnAzKontenjan { get; set; }

    [RegularExpression("^(id|ad|kontenjan)$", ErrorMessage = "Sıralama id, ad veya kontenjan olmalı.")]
    public string Sirala { get; set; } = "id";

    [RegularExpression("^(artan|azalan)$", ErrorMessage = "Yön artan veya azalan olmalı.")]
    public string Yon { get; set; } = "artan";
}
