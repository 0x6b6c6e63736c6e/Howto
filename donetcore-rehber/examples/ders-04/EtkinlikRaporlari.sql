-- Bağlantıda etkin veritabanı: DotNetAtolyeEtkinlikler
-- Ders 3'teki IlkKurulum migration'ı uygulanmış olmalı.

-- Örnek katılımcı ve kayıt verileri
IF OBJECT_ID(N'dbo.Etkinlikler', N'U') IS NULL
    THROW 50001, N'Ders 3 migration adımını tamamlayın ve doğru veritabanını seçin.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Etkinlikler WHERE Id = 1)
    OR NOT EXISTS (SELECT 1 FROM dbo.Etkinlikler WHERE Id = 2)
    THROW 50002, N'Rapor örnekleri Ders 3 başlangıç etkinliklerini gerektirir.', 1;

DROP TABLE IF EXISTS #AtolyeKayitlar;
DROP TABLE IF EXISTS #AtolyeKatilimcilar;

CREATE TABLE #AtolyeKatilimcilar (
    Id int NOT NULL PRIMARY KEY,
    Ad nvarchar(80) NOT NULL
);

CREATE TABLE #AtolyeKayitlar (
    EtkinlikId int NOT NULL,
    KatilimciId int NOT NULL,
    PRIMARY KEY (EtkinlikId, KatilimciId)
);

INSERT INTO #AtolyeKatilimcilar (Id, Ad)
VALUES (1, N'Deniz Kaya'), (2, N'Ece Arslan'), (3, N'Mert Yılmaz');

INSERT INTO #AtolyeKayitlar (EtkinlikId, KatilimciId)
VALUES (1, 1), (1, 2), (1, 3);

-- SELECT: 30 ve üzeri kontenjanı olan etkinlikler
SELECT e.Id, e.Ad, e.Kontenjan
FROM dbo.Etkinlikler AS e
WHERE e.Kontenjan >= 30
ORDER BY e.Kontenjan DESC, e.Id;

-- INNER JOIN: Etkinliğe kayıtlı katılımcılar
SELECT e.Ad AS Etkinlik, k.Ad AS Katilimci
FROM #AtolyeKayitlar AS r
INNER JOIN dbo.Etkinlikler AS e ON e.Id = r.EtkinlikId
INNER JOIN #AtolyeKatilimcilar AS k ON k.Id = r.KatilimciId
ORDER BY e.Id, k.Id;

-- LEFT JOIN ve GROUP BY: Etkinlik başına doluluk
SELECT
    e.Ad AS Etkinlik,
    e.Kontenjan,
    COUNT(r.KatilimciId) AS KayitSayisi,
    e.Kontenjan - COUNT(r.KatilimciId) AS KalanYer,
    CAST(100.0 * COUNT(r.KatilimciId) / e.Kontenjan AS decimal(5, 2)) AS DolulukYuzdesi
FROM dbo.Etkinlikler AS e
LEFT JOIN #AtolyeKayitlar AS r ON e.Id = r.EtkinlikId
GROUP BY e.Id, e.Ad, e.Kontenjan
ORDER BY e.Id;

-- HAVING: En az iki kaydı olan etkinlikler
SELECT e.Ad AS Etkinlik, COUNT(r.KatilimciId) AS KayitSayisi
FROM dbo.Etkinlikler AS e
LEFT JOIN #AtolyeKayitlar AS r ON e.Id = r.EtkinlikId
GROUP BY e.Id, e.Ad
HAVING COUNT(r.KatilimciId) >= 2
ORDER BY KayitSayisi DESC, e.Id;
