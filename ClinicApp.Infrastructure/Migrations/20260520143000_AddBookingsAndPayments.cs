using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520143000_AddBookingsAndPayments")]
    public partial class AddBookingsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Bookings](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [PatientId] UNIQUEIDENTIFIER NOT NULL,
        [DoctorId] UNIQUEIDENTIFIER NOT NULL,
        [ServiceId] UNIQUEIDENTIFIER NOT NULL,
        [AppointmentDate] DATE NOT NULL,
        [SlotStartTime] TIME NOT NULL,
        [SlotEndTime] TIME NOT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Bookings_Status] DEFAULT (N'Pending'),
        [PaymentStatus] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Bookings_PaymentStatus] DEFAULT (N'Unpaid'),
        [PaymentMode] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Bookings_PaymentMode] DEFAULT (N'Online'),
        [QueueNumber] INT NULL,
        [TotalFee] DECIMAL(10,2) NOT NULL,
        [ConsultationFeeSnapshot] DECIMAL(10,2) NOT NULL,
        [ServiceFeeSnapshot] DECIMAL(10,2) NOT NULL,
        [IsWalkIn] BIT NOT NULL CONSTRAINT [DF_Bookings_IsWalkIn] DEFAULT ((0)),
        [ProofType] NVARCHAR(20) NULL,
        [ProofValue] NVARCHAR(500) NULL,
        [ProofSubmittedAt] DATETIME2 NULL,
        [CancellationReason] NVARCHAR(500) NULL,
        [Notes] NVARCHAR(2000) NULL,
        [RescheduledFromBookingId] UNIQUEIDENTIFIER NULL,
        [ReceiptUrl] NVARCHAR(500) NULL,
        [OrNumber] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_RescheduledFromBookingId] FOREIGN KEY ([RescheduledFromBookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services]([Id]) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payments](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [BookingId] UNIQUEIDENTIFIER NOT NULL,
        [Amount] DECIMAL(10,2) NOT NULL,
        [PaymentMethod] NVARCHAR(20) NOT NULL,
        [ReferenceNumber] NVARCHAR(100) NULL,
        [ProofImageUrl] NVARCHAR(500) NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Payments_Status] DEFAULT (N'Unpaid'),
        [OrNumber] NVARCHAR(50) NULL,
        [VerifiedByUserId] NVARCHAR(450) NULL,
        [VerifiedAt] DATETIME2 NULL,
        [WaivedByUserId] NVARCHAR(450) NULL,
        [WaivedAt] DATETIME2 NULL,
        [WaivedReason] NVARCHAR(500) NULL,
        [RefundedByUserId] NVARCHAR(450) NULL,
        [RefundedAt] DATETIME2 NULL,
        [RefundReason] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Payments_AspNetUsers_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_AspNetUsers_WaivedByUserId] FOREIGN KEY ([WaivedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_AspNetUsers_RefundedByUserId] FOREIGN KEY ([RefundedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_DoctorId'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_DoctorId] ON [dbo].[Bookings] ([DoctorId]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_DoctorId_AppointmentDate'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_DoctorId_AppointmentDate] ON [dbo].[Bookings] ([DoctorId], [AppointmentDate]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_PatientId'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_PatientId] ON [dbo].[Bookings] ([PatientId]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_PaymentStatus'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_PaymentStatus] ON [dbo].[Bookings] ([PaymentStatus]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_QueueNumber'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_QueueNumber] ON [dbo].[Bookings] ([QueueNumber]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_RescheduledFromBookingId'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_RescheduledFromBookingId] ON [dbo].[Bookings] ([RescheduledFromBookingId]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_ServiceId'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_ServiceId] ON [dbo].[Bookings] ([ServiceId]);
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Bookings_Status'
      AND object_id = OBJECT_ID(N'[dbo].[Bookings]')
)
BEGIN
    CREATE INDEX [IX_Bookings_Status] ON [dbo].[Bookings] ([Status]);
END;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_BookingId'
      AND object_id = OBJECT_ID(N'[dbo].[Payments]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [dbo].[Payments] ([BookingId]);
END;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_RefundedByUserId'
      AND object_id = OBJECT_ID(N'[dbo].[Payments]')
)
BEGIN
    CREATE INDEX [IX_Payments_RefundedByUserId] ON [dbo].[Payments] ([RefundedByUserId]);
END;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_VerifiedByUserId'
      AND object_id = OBJECT_ID(N'[dbo].[Payments]')
)
BEGIN
    CREATE INDEX [IX_Payments_VerifiedByUserId] ON [dbo].[Payments] ([VerifiedByUserId]);
END;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_WaivedByUserId'
      AND object_id = OBJECT_ID(N'[dbo].[Payments]')
)
BEGIN
    CREATE INDEX [IX_Payments_WaivedByUserId] ON [dbo].[Payments] ([WaivedByUserId]);
END;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Payments];
END;

IF OBJECT_ID(N'[dbo].[Bookings]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Bookings];
END;
""");
        }
    }
}
