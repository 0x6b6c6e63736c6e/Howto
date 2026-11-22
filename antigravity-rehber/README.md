# Antigravity & Antigravity IDE Verimlilik Rehberi

Google Antigravity ve Antigravity IDE için hazırlanmış bağımsız, etkileşimli Türkçe web rehberi.

## Nasıl Açılır?

- **Doğrudan Tarayıcıda Açma:** `index.html` dosyasına çift tıklayarak veya tarayıcınıza sürükleyerek (`file://`) ek kurulum gerekmeden açabilirsiniz.
- **Yerel Sunucu ile Açma (İsteğe Bağlı):**

```sh
python3 -m http.server 8765
```

Ardından tarayıcınızda `http://localhost:8765` adresini ziyaret edin.

## Dosyalar

- `index.html` — Anlamsal HTML5 yapısı, Türkçe içerik, kontrol listesi ve kısayol modalı
- `styles.css` — Açık/koyu tema belirteçleri, kapsayıcı sorgulu tablolar, sekmeler ve yazdırılabilir stiller
- `app.js` — Tema geçişi, sekme/akordeon kontrolleri, komut arama/filtreleme, pano kopyalama ve `localStorage` ilerleme takibi
