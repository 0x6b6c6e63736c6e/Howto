# .NET Atölye

MVC 5 bilen geliştiriciler için ASP.NET Core, EF Core ve SQL Server eğitim sitesi.

## Yerel önizleme

```sh
python3 -m http.server 8080 --bind 127.0.0.1 --directory site
```

Site adresi `http://127.0.0.1:8080/`. Cloudflare için statik dosya dizini `site/`.

## İlk dersin ASP.NET Core örneği

.NET 10 SDK ile depo kökünde çalıştır:

```sh
dotnet run --project examples/Etkinlikler --no-launch-profile --urls http://localhost:5080
```

`http://localhost:5080/` veya `/Etkinlikler/Index` adresinde iki etkinlik görünür.

## İkinci dersin ASP.NET Core örneği

```sh
dotnet run --project examples/ders-02/Etkinlikler --no-launch-profile --urls http://localhost:5081
```

Listeyi `http://localhost:5081/`, formu `http://localhost:5081/Etkinlikler/Ekle` adresinden aç. Kayıtlar uygulama belleğinde tutulur.

## Cloudflare Pages

`dotnetatolye` projesinde yayın kökü `site/`. Git entegrasyonunda build komutu `exit 0`, çıktı dizini `site`. Direct Upload seçilirse `site/` klasörünü veya kökünde site dosyaları olan ZIP paketini yükle. İndirilebilir ikinci ders örneği `site/ornekler/` içinde yer alır.

## Üçüncü dersin ASP.NET Core ve EF Core örneği

`examples/ders-03/Etkinlikler` dizininden çalıştır. .NET 10 SDK ve ayrı bir geliştirici SQL Server veritabanı gerekir. İlk migration örnek projede hazırdır.

```sh
dotnet restore
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:EtkinlikDb" "Server=SUNUCU;Database=DotNetAtolyeEtkinlikler;User Id=KULLANICI;Password=PAROLA;Encrypt=True"
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5082
```

PowerShell’de önce `$env:ASPNETCORE_ENVIRONMENT = "Development"` çalıştır ve komutların başındaki ortam değişkeni atamasını çıkar. Uygulama adresi `http://localhost:5082/`.

## Beşinci dersin EF Core doluluk raporu

`examples/ders-05/Etkinlikler` dizininden, üçüncü dersteki SQL Server bağlantısı ile:

```sh
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5083
```

Rapor adresi `http://localhost:5083/Etkinlikler/Doluluk`. SQL Server’a bağlanmadan sorgu metnini görmek için `dotnet run --no-launch-profile -- --rapor-sql` çalıştır.

## Altıncı dersin eşzamanlı kayıt örneği

`examples/ders-06/Etkinlikler` dizininden, beşinci dersteki geliştirici SQL Server veritabanıyla:

```sh
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5084
```

Form adresi `http://localhost:5084/Kayitlar/Ekle`. Eşzamanlı son yer deneyini `ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile -- --kayit-deneyi` komutuyla çalıştır. Deney her çalıştırmada geliştirici veritabanına bir test etkinliği ekler.


## On sekizinci dersin yayın çıktıları

Eğitim sitesi için `dotnetatolye-pages.zip`, etkinlik uygulaması için `releases/ders-18/etkinlikler-release.zip` ve incelenebilir SQL Server migration betiği `releases/ders-18/migrations.sql` hazırlandı. Release paketi .NET 10 ASP.NET Core runtime gerektirir. SQL Server üzerinde migration uygulaması ve kimlik doğrulama testi ayrıca yapılmalıdır. Üretimde JWT vermek için OAuth/OIDC sağlayıcısı entegrasyonu gerekir.
