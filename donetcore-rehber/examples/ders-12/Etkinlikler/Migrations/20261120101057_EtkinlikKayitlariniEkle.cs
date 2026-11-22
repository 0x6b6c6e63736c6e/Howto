using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Etkinlikler.Migrations
{
    /// <inheritdoc />
    public partial class EtkinlikKayitlariniEkle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Katilimcilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Katilimcilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EtkinlikKayitlari",
                columns: table => new
                {
                    EtkinlikId = table.Column<int>(type: "int", nullable: false),
                    KatilimciId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtkinlikKayitlari", x => new { x.EtkinlikId, x.KatilimciId });
                    table.ForeignKey(
                        name: "FK_EtkinlikKayitlari_Etkinlikler_EtkinlikId",
                        column: x => x.EtkinlikId,
                        principalTable: "Etkinlikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EtkinlikKayitlari_Katilimcilar_KatilimciId",
                        column: x => x.KatilimciId,
                        principalTable: "Katilimcilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Katilimcilar",
                columns: new[] { "Id", "Ad" },
                values: new object[,]
                {
                    { 1, "Deniz Kaya" },
                    { 2, "Ece Arslan" },
                    { 3, "Mert Yılmaz" }
                });

            migrationBuilder.InsertData(
                table: "EtkinlikKayitlari",
                columns: new[] { "EtkinlikId", "KatilimciId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 1, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtkinlikKayitlari_KatilimciId",
                table: "EtkinlikKayitlari",
                column: "KatilimciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtkinlikKayitlari");

            migrationBuilder.DropTable(
                name: "Katilimcilar");
        }
    }
}
