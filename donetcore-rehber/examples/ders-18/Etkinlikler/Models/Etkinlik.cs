using System.Text.Json.Serialization;

namespace Etkinlikler.Models;

public sealed class Etkinlik
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int Kontenjan { get; set; }
    public string? Aciklama { get; set; }

    [JsonIgnore]
    public ICollection<EtkinlikKaydi> Kayitlar { get; set; } = new List<EtkinlikKaydi>();
}
