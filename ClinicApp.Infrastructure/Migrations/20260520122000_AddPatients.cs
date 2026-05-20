using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520122000_AddPatients")]
    public partial class AddPatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Patients]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Patients](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [PatientCode] NVARCHAR(20) NOT NULL,
        [UserId] NVARCHAR(450) NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [MiddleName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [DateOfBirth] DATE NOT NULL,
        [Sex] NVARCHAR(10) NOT NULL,
        [CivilStatus] NVARCHAR(20) NULL,
        [Address] NVARCHAR(300) NULL,
        [City] NVARCHAR(100) NULL,
        [ZipCode] NVARCHAR(10) NULL,
        [ContactNumber] NVARCHAR(20) NULL,
        [Email] NVARCHAR(200) NULL,
        [EmergencyContactName] NVARCHAR(200) NULL,
        [EmergencyContactNumber] NVARCHAR(20) NULL,
        [EmergencyContactRelationship] NVARCHAR(50) NULL,
        [BloodType] NVARCHAR(5) NULL,
        [PhilHealthNumber] NVARCHAR(20) NULL,
        [HmoProvider] NVARCHAR(100) NULL,
        [HmoCardNumber] NVARCHAR(50) NULL,
        [IsGuest] BIT NOT NULL CONSTRAINT [DF_Patients_IsGuest] DEFAULT ((0)),
        [IsEmailVerified] BIT NOT NULL CONSTRAINT [DF_Patients_IsEmailVerified] DEFAULT ((0)),
        [ConsentedAt] DATETIME2 NULL,
        [ConsentVersion] NVARCHAR(10) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'[dbo].[Patients]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Patients_PatientCode'
      AND object_id = OBJECT_ID(N'[dbo].[Patients]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_PatientCode] ON [dbo].[Patients] ([PatientCode]);
END;

IF OBJECT_ID(N'[dbo].[Patients]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Patients_UserId'
      AND object_id = OBJECT_ID(N'[dbo].[Patients]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_UserId] ON [dbo].[Patients] ([UserId]) WHERE [UserId] IS NOT NULL;
END;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Patients]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Patients];
END;
""");
        }
    }
}
