using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5CClinicQueueAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckedInByUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoctorCompletedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorCompletedByUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorFeeNotes",
                table: "Bookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfessionalFeeWaived",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfessionalFeeWaivedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalFeeWaivedByUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalFeeWaivedReason",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoapNotes",
                table: "Bookings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingServiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingServiceItems_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingServiceItems_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CheckedInByUserId",
                table: "Bookings",
                column: "CheckedInByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DoctorCompletedByUserId",
                table: "Bookings",
                column: "DoctorCompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProfessionalFeeWaivedByUserId",
                table: "Bookings",
                column: "ProfessionalFeeWaivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServiceItems_BookingId",
                table: "BookingServiceItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServiceItems_ServiceId",
                table: "BookingServiceItems",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_CheckedInByUserId",
                table: "Bookings",
                column: "CheckedInByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorCompletedByUserId",
                table: "Bookings",
                column: "DoctorCompletedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_ProfessionalFeeWaivedByUserId",
                table: "Bookings",
                column: "ProfessionalFeeWaivedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorCompletedByUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_ProfessionalFeeWaivedByUserId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingServiceItems");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_DoctorCompletedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ProfessionalFeeWaivedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DoctorCompletedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DoctorCompletedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DoctorFeeNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsProfessionalFeeWaived",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProfessionalFeeWaivedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProfessionalFeeWaivedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProfessionalFeeWaivedReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SoapNotes",
                table: "Bookings");
        }
    }
}
