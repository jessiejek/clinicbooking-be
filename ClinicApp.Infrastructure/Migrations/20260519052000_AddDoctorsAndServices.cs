using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AppDbContext))]
    [Migration("20260519052000_AddDoctorsAndServices")]
    public partial class AddDoctorsAndServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Doctors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Doctors](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Specialization] NVARCHAR(200) NOT NULL,
        [Bio] NVARCHAR(MAX) NULL,
        [ProfilePhotoUrl] NVARCHAR(500) NULL,
        [LicenseNumber] NVARCHAR(50) NULL,
        [PtrNumber] NVARCHAR(50) NULL,
        [S2Number] NVARCHAR(50) NULL,
        [ConsultationFee] DECIMAL(10,2) NOT NULL,
        [SlotDurationMinutes] INT NOT NULL CONSTRAINT [DF_Doctors_SlotDurationMinutes] DEFAULT ((30)),
        [SlotCapacity] INT NOT NULL CONSTRAINT [DF_Doctors_SlotCapacity] DEFAULT ((1)),
        [DailyPatientLimit] INT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Doctors_Status] DEFAULT (N'Active'),
        [AverageRating] DECIMAL(3,2) NULL,
        [ReviewCount] INT NOT NULL CONSTRAINT [DF_Doctors_ReviewCount] DEFAULT ((0)),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Doctors_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'[dbo].[DoctorSchedules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorSchedules](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [DoctorId] UNIQUEIDENTIFIER NOT NULL,
        [DayOfWeek] NVARCHAR(10) NOT NULL,
        [StartTime] TIME NOT NULL,
        [EndTime] TIME NOT NULL,
        CONSTRAINT [PK_DoctorSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[DoctorBlockedDates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorBlockedDates](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [DoctorId] UNIQUEIDENTIFIER NOT NULL,
        [BlockedDate] DATE NOT NULL,
        [Reason] NVARCHAR(300) NULL,
        CONSTRAINT [PK_DoctorBlockedDates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorBlockedDates_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[DoctorDayStatuses]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorDayStatuses](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [DoctorId] UNIQUEIDENTIFIER NOT NULL,
        [Date] DATE NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [RunningLateMinutes] INT NULL,
        CONSTRAINT [PK_DoctorDayStatuses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorDayStatuses_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[Services]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Services](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Category] NVARCHAR(30) NOT NULL,
        [Price] DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Services_Price] DEFAULT ((0)),
        [EstimatedDurationMinutes] INT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Services_IsActive] DEFAULT ((1)),
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[DoctorServices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorServices](
        [DoctorId] UNIQUEIDENTIFIER NOT NULL,
        [ServiceId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_DoctorServices] PRIMARY KEY ([DoctorId], [ServiceId]),
        CONSTRAINT [FK_DoctorServices_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DoctorServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services]([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[Doctors]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Doctors_UserId'
      AND object_id = OBJECT_ID(N'[dbo].[Doctors]')
)
BEGIN
    CREATE INDEX [IX_Doctors_UserId] ON [dbo].[Doctors] ([UserId]);
END;

IF OBJECT_ID(N'[dbo].[DoctorSchedules]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DoctorSchedules_DoctorId'
      AND object_id = OBJECT_ID(N'[dbo].[DoctorSchedules]')
)
BEGIN
    CREATE INDEX [IX_DoctorSchedules_DoctorId] ON [dbo].[DoctorSchedules] ([DoctorId]);
END;

IF OBJECT_ID(N'[dbo].[DoctorBlockedDates]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DoctorBlockedDates_DoctorId'
      AND object_id = OBJECT_ID(N'[dbo].[DoctorBlockedDates]')
)
BEGIN
    CREATE INDEX [IX_DoctorBlockedDates_DoctorId] ON [dbo].[DoctorBlockedDates] ([DoctorId]);
END;

IF OBJECT_ID(N'[dbo].[DoctorDayStatuses]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DoctorDayStatuses_DoctorId_Date'
      AND object_id = OBJECT_ID(N'[dbo].[DoctorDayStatuses]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_DoctorDayStatuses_DoctorId_Date] ON [dbo].[DoctorDayStatuses] ([DoctorId], [Date]);
END;

IF OBJECT_ID(N'[dbo].[DoctorServices]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DoctorServices_ServiceId'
      AND object_id = OBJECT_ID(N'[dbo].[DoctorServices]')
)
BEGIN
    CREATE INDEX [IX_DoctorServices_ServiceId] ON [dbo].[DoctorServices] ([ServiceId]);
END;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[DoctorServices]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[DoctorServices];
END;

IF OBJECT_ID(N'[dbo].[DoctorDayStatuses]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[DoctorDayStatuses];
END;

IF OBJECT_ID(N'[dbo].[DoctorBlockedDates]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[DoctorBlockedDates];
END;

IF OBJECT_ID(N'[dbo].[DoctorSchedules]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[DoctorSchedules];
END;

IF OBJECT_ID(N'[dbo].[Doctors]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Doctors];
END;

IF OBJECT_ID(N'[dbo].[Services]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Services];
END;
""");
        }
    }
}
