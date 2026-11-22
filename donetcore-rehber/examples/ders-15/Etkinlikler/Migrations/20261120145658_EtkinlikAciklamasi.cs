using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Etkinlikler.Migrations
{
    /// <inheritdoc />
    public partial class EtkinlikAciklamasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Etkinlikler",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Etkinlikler");
        }
    }
}
