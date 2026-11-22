namespace Etkinlikler.Models;

public sealed class EtkinlikKaydi
{
    public int EtkinlikId { get; set; }
    public int KatilimciId { get; set; }
    public Etkinlik Etkinlik { get; set; } = null!;
    public Katilimci Katilimci { get; set; } = null!;
}
