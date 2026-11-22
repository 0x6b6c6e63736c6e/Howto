using Etkinlikler.Models;
using Microsoft.EntityFrameworkCore;

namespace Etkinlikler.Data;

public sealed class EtkinlikDbContext(DbContextOptions<EtkinlikDbContext> options)
    : DbContext(options)
{
    public DbSet<Etkinlik> Etkinlikler => Set<Etkinlik>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var etkinlik = modelBuilder.Entity<Etkinlik>();

        etkinlik.ToTable("Etkinlikler", table =>
            table.HasCheckConstraint("CK_Etkinlikler_Kontenjan", "[Kontenjan] BETWEEN 1 AND 500"));

        etkinlik.Property(item => item.Ad).HasMaxLength(80).IsRequired();

        etkinlik.HasData(
            new Etkinlik { Id = 1, Ad = "Web Geliştirme Atölyesi", Kontenjan = 25 },
            new Etkinlik { Id = 2, Ad = "SQL Server Semineri", Kontenjan = 40 });
    }
}
