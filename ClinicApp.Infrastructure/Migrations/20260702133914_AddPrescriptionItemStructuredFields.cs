using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionItemStructuredFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dose",
                table: "PrescriptionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyCode",
                table: "PrescriptionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteDescription",
                table: "PrescriptionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "PrescriptionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasureDescription",
                table: "PrescriptionItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dose",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "FrequencyCode",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "RouteDescription",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureDescription",
                table: "PrescriptionItems");
        }
    }
}
