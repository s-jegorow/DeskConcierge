using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskConcierge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Amount",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AmountConfidence",
                table: "Documents",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "DateConfidence",
                table: "Documents",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "IbanConfidence",
                table: "Documents",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "InvoiceNumberConfidence",
                table: "Documents",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AmountConfidence",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DateConfidence",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IbanConfidence",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "InvoiceNumberConfidence",
                table: "Documents");
        }
    }
}
