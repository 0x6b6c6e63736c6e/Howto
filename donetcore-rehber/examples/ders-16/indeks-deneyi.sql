-- Aynı oturumda çalıştırın. #EtkinlikDeneyi oturum kapanınca silinir.
-- Mevcut kalıcı tablolara veya indekslere dokunulmaz.
DROP TABLE IF EXISTS #EtkinlikDeneyi;
CREATE TABLE #EtkinlikDeneyi
(
    Id int NOT NULL PRIMARY KEY,
    Ad nvarchar(80) NOT NULL,
    Kontenjan int NOT NULL
);
INSERT INTO #EtkinlikDeneyi (Id, Ad, Kontenjan)
SELECT Id, Ad, Kontenjan FROM dbo.Etkinlikler;

-- SSMS'te Include Actual Execution Plan seçeneğini açın.
-- Messages sekmesindeki logical reads ve süreyi kaydedin.
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT Id, Ad, Kontenjan
FROM #EtkinlikDeneyi
WHERE Kontenjan >= 30
ORDER BY Kontenjan, Id;

-- Karşılaştırılacak geçici indeks
CREATE INDEX IX_Deney_Kontenjan_Id ON #EtkinlikDeneyi (Kontenjan, Id) INCLUDE (Ad);

SELECT Id, Ad, Kontenjan
FROM #EtkinlikDeneyi
WHERE Kontenjan >= 30
ORDER BY Kontenjan, Id;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
