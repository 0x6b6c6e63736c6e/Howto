namespace Etkinlikler.Models;

public sealed record EtkinlikDolulugu(
    string Etkinlik, int Kontenjan, int KayitSayisi, int KalanYer, decimal DolulukYuzdesi);
