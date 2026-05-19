using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[ClinicSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClinicSettings](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [ClinicName] NVARCHAR(200) NOT NULL,
        [LogoUrl] NVARCHAR(500) NULL,
        [PrimaryColor] NVARCHAR(10) NOT NULL CONSTRAINT [DF_ClinicSettings_PrimaryColor] DEFAULT (N'#5D3E8E'),
        [SecondaryColor] NVARCHAR(10) NOT NULL CONSTRAINT [DF_ClinicSettings_SecondaryColor] DEFAULT (N'#2563EB'),
        [Address] NVARCHAR(300) NULL,
        [Phone] NVARCHAR(20) NULL,
        [ContactEmail] NVARCHAR(200) NULL,
        [FacebookUrl] NVARCHAR(300) NULL,
        [InstagramUrl] NVARCHAR(300) NULL,
        [OperatingHoursJson] NVARCHAR(4000) NOT NULL CONSTRAINT [DF_ClinicSettings_OperatingHoursJson] DEFAULT (N'{}'),
        [CancellationDeadlineHours] INT NOT NULL CONSTRAINT [DF_ClinicSettings_CancellationDeadlineHours] DEFAULT ((24)),
        [PatientPortalEnabled] BIT NOT NULL CONSTRAINT [DF_ClinicSettings_PatientPortalEnabled] DEFAULT ((1)),
        [VaccinationReminderEnabled] BIT NOT NULL CONSTRAINT [DF_ClinicSettings_VaccinationReminderEnabled] DEFAULT ((1)),
        [FollowUpReminderEnabled] BIT NOT NULL CONSTRAINT [DF_ClinicSettings_FollowUpReminderEnabled] DEFAULT ((1)),
        [IsPayAtClinicMode] BIT NOT NULL CONSTRAINT [DF_ClinicSettings_IsPayAtClinicMode] DEFAULT ((0)),
        [PayAtClinicNoShowWindowMinutes] INT NOT NULL CONSTRAINT [DF_ClinicSettings_PayAtClinicNoShowWindowMinutes] DEFAULT ((60)),
        [PrivacyPolicyText] NVARCHAR(4000) NULL,
        [ConsentVersion] NVARCHAR(10) NOT NULL CONSTRAINT [DF_ClinicSettings_ConsentVersion] DEFAULT (N'v1.0'),
        [GcashAccountName] NVARCHAR(100) NULL,
        [GcashNumber] NVARCHAR(20) NULL,
        [GcashQrImageUrl] NVARCHAR(500) NULL,
        [MayaAccountName] NVARCHAR(100) NULL,
        [MayaNumber] NVARCHAR(20) NULL,
        [MayaQrImageUrl] NVARCHAR(500) NULL,
        [BankName] NVARCHAR(100) NULL,
        [BankAccountName] NVARCHAR(100) NULL,
        [BankAccountNumber] NVARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ClinicSettings] PRIMARY KEY ([Id])
    );
END;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[ClinicSettings]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ClinicSettings];
END;
""");
        }
    }
}
