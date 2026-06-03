using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskConcierge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "OcrConfidence",
                table: "Documents",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrText",
                table: "Documents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OcrText",
                table: "Documents");
        }
    }
}
