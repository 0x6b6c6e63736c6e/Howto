namespace Etkinlikler.Models;

public sealed class Katilimci
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public ICollection<EtkinlikKaydi> Kayitlar { get; set; } = new List<EtkinlikKaydi>();
}
