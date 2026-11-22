namespace Etkinlikler.Models;

public sealed class Etkinlik
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int Kontenjan { get; set; }
}
