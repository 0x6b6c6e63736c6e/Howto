using Etkinlikler.Models;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Data;

public sealed class EtkinlikDbContext(DbContextOptions<EtkinlikDbContext> options)
    : DbContext(options)
{
    public DbSet<Etkinlik> Etkinlikler => Set<Etkinlik>();

    public DbSet<EtkinlikKaydi> EtkinlikKayitlari => Set<EtkinlikKaydi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var etkinlik = modelBuilder.Entity<Etkinlik>();

        etkinlik.ToTable("Etkinlikler", table =>
            table.HasCheckConstraint("CK_Etkinlikler_Kontenjan", "[Kontenjan] BETWEEN 1 AND 500"));

        etkinlik.Property(item => item.Ad).HasMaxLength(80).IsRequired();

        etkinlik.HasData(
            new Etkinlik { Id = 1, Ad = "Web Geliştirme Atölyesi", Kontenjan = 25 },
            new Etkinlik { Id = 2, Ad = "SQL Server Semineri", Kontenjan = 40 });

        var katilimci = modelBuilder.Entity<Katilimci>();
        katilimci.ToTable("Katilimcilar");
        katilimci.Property(item => item.Ad).HasMaxLength(80).IsRequired();
        katilimci.HasData(
            new Katilimci { Id = 1, Ad = "Deniz Kaya" },
            new Katilimci { Id = 2, Ad = "Ece Arslan" },
            new Katilimci { Id = 3, Ad = "Mert Yılmaz" });

        var kayit = modelBuilder.Entity<EtkinlikKaydi>();
        kayit.ToTable("EtkinlikKayitlari");
        kayit.HasKey(item => new { item.EtkinlikId, item.KatilimciId });
        kayit.HasOne<Etkinlik>().WithMany().HasForeignKey(item => item.EtkinlikId);
        kayit.HasOne<Katilimci>().WithMany().HasForeignKey(item => item.KatilimciId);
        kayit.HasData(
            new EtkinlikKaydi { EtkinlikId = 1, KatilimciId = 1 },
            new EtkinlikKaydi { EtkinlikId = 1, KatilimciId = 2 },
            new EtkinlikKaydi { EtkinlikId = 1, KatilimciId = 3 });
    }
}
