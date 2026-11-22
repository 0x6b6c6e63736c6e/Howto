-- Ders 5 ve sonraki migration'ları uygulanmış geliştirici veritabanını seçin.
-- Rapor 1: CTE ile doluluk ve sıralama
;WITH Doluluk AS
(
    SELECT e.Id, e.Ad, e.Kontenjan, COUNT(k.KatilimciId) AS KayitSayisi
    FROM dbo.Etkinlikler AS e
    LEFT JOIN dbo.EtkinlikKayitlari AS k ON k.EtkinlikId = e.Id
    GROUP BY e.Id, e.Ad, e.Kontenjan
)
SELECT Id, Ad, Kontenjan, KayitSayisi,
       Kontenjan - KayitSayisi AS KalanYer,
       CAST(100.0 * KayitSayisi / Kontenjan AS decimal(5, 2)) AS DolulukYuzdesi,
       DENSE_RANK() OVER (ORDER BY KayitSayisi DESC) AS KayitSirasi
FROM Doluluk
ORDER BY KayitSirasi, Id;

-- Rapor 2: Her katılımcının kayıtları ve toplam etkinlik sayısı
SELECT k.Id AS KatilimciId, k.Ad AS Katilimci, e.Id AS EtkinlikId, e.Ad AS Etkinlik,
       ROW_NUMBER() OVER (PARTITION BY k.Id ORDER BY e.Id) AS Sira,
       COUNT(*) OVER (PARTITION BY k.Id) AS EtkinlikSayisi
FROM dbo.Katilimcilar AS k
JOIN dbo.EtkinlikKayitlari AS r ON r.KatilimciId = k.Id
JOIN dbo.Etkinlikler AS e ON e.Id = r.EtkinlikId
ORDER BY k.Id, e.Id;

-- Rapor 3: Kimlik sırasına göre kümülatif kayıt sayısı
;WITH KayitOzeti AS
(
    SELECT e.Id, e.Ad, COUNT(r.KatilimciId) AS KayitSayisi
    FROM dbo.Etkinlikler AS e
    LEFT JOIN dbo.EtkinlikKayitlari AS r ON r.EtkinlikId = e.Id
    GROUP BY e.Id, e.Ad
)
SELECT Id, Ad, KayitSayisi,
       SUM(KayitSayisi) OVER (ORDER BY Id ROWS UNBOUNDED PRECEDING) AS KumulatifKayit
FROM KayitOzeti
ORDER BY Id;
