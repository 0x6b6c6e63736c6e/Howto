namespace Etkinlikler.Models;

public sealed record Etkinlik(string Ad, int Kontenjan);

public sealed class EtkinlikListesi
{
    private readonly object kilit = new();
    private readonly List<Etkinlik> etkinlikler =
    [
        new("Web Geliştirme Atölyesi", 25),
        new("SQL Server Semineri", 40)
    ];

    public IReadOnlyList<Etkinlik> Listele()
    {
        lock (kilit)
        {
            return etkinlikler.ToArray();
        }
    }

    public void Ekle(Etkinlik etkinlik)
    {
        lock (kilit)
        {
            etkinlikler.Add(etkinlik);
        }
    }
}
