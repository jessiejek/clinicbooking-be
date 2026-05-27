using ClinicApp.Domain.Entities.Authentication;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorBlockedDate> DoctorBlockedDates => Set<DoctorBlockedDate>();
    public DbSet<DoctorDayStatus> DoctorDayStatuses => Set<DoctorDayStatus>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<DoctorService> DoctorServices => Set<DoctorService>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingServiceItem> BookingServiceItems => Set<BookingServiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationVitalSign> ConsultationVitalSigns => Set<ConsultationVitalSign>();
    public DbSet<ConsultationSoapNote> ConsultationSoapNotes => Set<ConsultationSoapNote>();
    public DbSet<ConsultationDiagnosis> ConsultationDiagnoses => Set<ConsultationDiagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ConsultationFollowUp> ConsultationFollowUps => Set<ConsultationFollowUp>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StaffInvite> StaffInvites => Set<StaffInvite>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<ExternalLoginAccount> ExternalLoginAccounts => Set<ExternalLoginAccount>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();
    public DbSet<PatientVaccination> PatientVaccinations => Set<PatientVaccination>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Role).IsRequired().HasMaxLength(20);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.AuthProvider).HasMaxLength(20);
            entity.Property(x => x.ProviderUserId).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        builder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Specialization).IsRequired().HasMaxLength(200);
            entity.Property(x => x.ProfilePhotoUrl).HasMaxLength(500);
            entity.Property(x => x.LicenseNumber).HasMaxLength(50);
            entity.Property(x => x.PtrNumber).HasMaxLength(50);
            entity.Property(x => x.S2Number).HasMaxLength(50);
            entity.Property(x => x.ConsultationFee).HasPrecision(10, 2);
            entity.Property(x => x.SlotDurationMinutes).HasDefaultValue(30);
            entity.Property(x => x.SlotCapacity).HasDefaultValue(1);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(x => x.AverageRating).HasPrecision(3, 2);
            entity.Property(x => x.ReviewCount).HasDefaultValue(0);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DoctorSchedule>(entity =>
        {
            entity.ToTable("DoctorSchedules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DayOfWeek).IsRequired().HasMaxLength(10);
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.HasIndex(x => x.DoctorId);
            entity.HasOne<Doctor>()
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DoctorBlockedDate>(entity =>
        {
            entity.ToTable("DoctorBlockedDates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(300);
            entity.Property(x => x.BlockedDate).HasColumnType("date");
            entity.HasIndex(x => x.DoctorId);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DoctorDayStatus>(entity =>
        {
            entity.ToTable("DoctorDayStatuses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).HasColumnType("date");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(x => new { x.DoctorId, x.Date }).IsUnique();
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Price).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        builder.Entity<DoctorService>(entity =>
        {
            entity.ToTable("DoctorServices");
            entity.HasKey(x => new { x.DoctorId, x.ServiceId });
            entity.HasIndex(x => x.ServiceId);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Service>()
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientCode).IsRequired().HasMaxLength(20);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.MiddleName).HasMaxLength(100);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.DateOfBirth).HasColumnType("date");
            entity.Property(x => x.Sex).IsRequired().HasMaxLength(10);
            entity.Property(x => x.CivilStatus).HasMaxLength(20);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.ZipCode).HasMaxLength(10);
            entity.Property(x => x.ContactNumber).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(200);
            entity.Property(x => x.EmergencyContactNumber).HasMaxLength(20);
            entity.Property(x => x.EmergencyContactRelationship).HasMaxLength(50);
            entity.Property(x => x.BloodType).HasMaxLength(5);
            entity.Property(x => x.PhilHealthNumber).HasMaxLength(20);
            entity.Property(x => x.HmoProvider).HasMaxLength(100);
            entity.Property(x => x.HmoCardNumber).HasMaxLength(50);
            entity.Property(x => x.IsGuest).HasDefaultValue(false);
            entity.Property(x => x.IsEmailVerified).HasDefaultValue(false);
            entity.Property(x => x.ConsentedAt).HasColumnType("datetime2");
            entity.Property(x => x.ConsentVersion).HasMaxLength(10);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.PatientCode).IsUnique();
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.DoctorId).IsRequired();
            entity.Property(x => x.ServiceId).IsRequired();
            entity.Property(x => x.AppointmentDate).HasColumnType("date");
            entity.Property(x => x.SlotStartTime).HasColumnType("time");
            entity.Property(x => x.SlotEndTime).HasColumnType("time");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(x => x.PaymentStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Unpaid");
            entity.Property(x => x.PaymentMode).IsRequired().HasMaxLength(20).HasDefaultValue("Online");
            entity.Property(x => x.QueueNumber);
            entity.Property(x => x.TotalFee).HasPrecision(10, 2);
            entity.Property(x => x.ConsultationFeeSnapshot).HasPrecision(10, 2);
            entity.Property(x => x.ServiceFeeSnapshot).HasPrecision(10, 2);
            entity.Property(x => x.IsWalkIn).HasDefaultValue(false);
            entity.Property(x => x.ProofType).HasMaxLength(20);
            entity.Property(x => x.ProofValue).HasMaxLength(500);
            entity.Property(x => x.ProofSubmittedAt).HasColumnType("datetime2");
            entity.Property(x => x.CancellationReason).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.RescheduledFromBookingId);
            entity.Property(x => x.ReceiptUrl).HasMaxLength(500);
            entity.Property(x => x.OrNumber).HasMaxLength(50);
            entity.Property(x => x.CheckedInAt).HasColumnType("datetime2");
            entity.Property(x => x.CheckedInByUserId).HasMaxLength(450);
            entity.Property(x => x.DoctorCompletedAt).HasColumnType("datetime2");
            entity.Property(x => x.DoctorCompletedByUserId).HasMaxLength(450);
            entity.Property(x => x.FinalAmount).HasPrecision(10, 2);
            entity.Property(x => x.Diagnosis).HasMaxLength(2000);
            entity.Property(x => x.DoctorFeeNotes).HasMaxLength(2000);
            entity.Property(x => x.SoapNotes).HasMaxLength(4000);
            entity.Property(x => x.PrescriptionJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.FollowUpDate).HasColumnType("date");
            entity.Property(x => x.FollowUpInstructions).HasMaxLength(4000);
            entity.Property(x => x.IsProfessionalFeeWaived).HasDefaultValue(false);
            entity.Property(x => x.ProfessionalFeeWaivedReason).HasMaxLength(500);
            entity.Property(x => x.ProfessionalFeeWaivedByUserId).HasMaxLength(450);
            entity.Property(x => x.ProfessionalFeeWaivedAt).HasColumnType("datetime2");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.AmountDue).HasPrecision(10, 2);
            entity.Property(x => x.CancelledAt).HasColumnType("datetime2");
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => x.ServiceId);
            entity.HasIndex(x => new { x.DoctorId, x.AppointmentDate });
            entity.HasIndex(x => x.QueueNumber);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.PaymentStatus);
            entity.HasIndex(x => x.RescheduledFromBookingId);
            entity.HasIndex(x => x.CheckedInByUserId);
            entity.HasIndex(x => x.DoctorCompletedByUserId);
            entity.HasIndex(x => x.ProfessionalFeeWaivedByUserId);
            entity.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RescheduledFromBooking)
                .WithMany()
                .HasForeignKey(x => x.RescheduledFromBookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CheckedInByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.DoctorCompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ProfessionalFeeWaivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BookingServiceItem>(entity =>
        {
            entity.ToTable("BookingServiceItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BookingId).IsRequired();
            entity.Property(x => x.ServiceId).IsRequired();
            entity.Property(x => x.ServiceNameSnapshot).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.ServiceId);
            entity.HasOne(x => x.Booking)
                .WithMany(x => x.BookingServiceItems)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BookingId).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(20);
            entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.ProofImageUrl).HasMaxLength(500);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Unpaid");
            entity.Property(x => x.OrNumber).HasMaxLength(50);
            entity.Property(x => x.VerifiedByUserId).HasMaxLength(450);
            entity.Property(x => x.VerifiedAt).HasColumnType("datetime2");
            entity.Property(x => x.WaivedByUserId).HasMaxLength(450);
            entity.Property(x => x.WaivedAt).HasColumnType("datetime2");
            entity.Property(x => x.WaivedReason).HasMaxLength(500);
            entity.Property(x => x.RefundedByUserId).HasMaxLength(450);
            entity.Property(x => x.RefundedAt).HasColumnType("datetime2");
            entity.Property(x => x.RefundReason).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.HasIndex(x => x.VerifiedByUserId);
            entity.HasIndex(x => x.WaivedByUserId);
            entity.HasIndex(x => x.RefundedByUserId);
            entity.HasOne(x => x.Booking)
                .WithOne(x => x.Payment)
                .HasForeignKey<Payment>(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.WaivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.RefundedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Consultation>(entity =>
        {
            entity.ToTable("Consultations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.DoctorId);
            entity.Property(x => x.BookingId);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
            entity.Property(x => x.GeneralNotes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.StartedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.CompletedAt).HasColumnType("datetime2");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => x.BookingId).IsUnique().HasFilter("[BookingId] IS NOT NULL");
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConsultationVitalSign>(entity =>
        {
            entity.ToTable("ConsultationVitalSigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.SystolicBp);
            entity.Property(x => x.DiastolicBp);
            entity.Property(x => x.HeartRate);
            entity.Property(x => x.RespiratoryRate);
            entity.Property(x => x.Temperature).HasPrecision(4, 1);
            entity.Property(x => x.OxygenSaturation);
            entity.Property(x => x.Weight).HasPrecision(10, 2);
            entity.Property(x => x.Height).HasPrecision(10, 2);
            entity.Property(x => x.Bmi).HasPrecision(10, 2);
            entity.Property(x => x.PainScore);
            entity.Property(x => x.TakenAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.TakenByUserId).HasMaxLength(450);
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.TakenByUserId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.TakenByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConsultationSoapNote>(entity =>
        {
            entity.ToTable("ConsultationSoapNotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.ConsultationId).IsRequired();
            entity.Property(x => x.Subjective).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Objective).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Assessment).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Plan).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId).IsUnique();
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithOne()
                .HasForeignKey<ConsultationSoapNote>(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConsultationDiagnosis>(entity =>
        {
            entity.ToTable("ConsultationDiagnoses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.ConsultationId).IsRequired();
            entity.Property(x => x.DiagnosisText).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.DiagnosisCode).HasMaxLength(100);
            entity.Property(x => x.IsPrimary).HasDefaultValue(false);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.ConsultationId).IsUnique().HasFilter("[IsPrimary] = 1");
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Prescription>(entity =>
        {
            entity.ToTable("Prescriptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.DoctorId);
            entity.Property(x => x.BookingId);
            entity.Property(x => x.ConsultationId);
            entity.Property(x => x.PrescriptionNumber).HasMaxLength(50);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.IssuedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PrescriptionItem>(entity =>
        {
            entity.ToTable("PrescriptionItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PrescriptionId).IsRequired();
            entity.Property(x => x.MedicineName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Strength).HasMaxLength(100);
            entity.Property(x => x.DosageForm).HasMaxLength(100);
            entity.Property(x => x.Route).HasMaxLength(50);
            entity.Property(x => x.Frequency).HasMaxLength(50);
            entity.Property(x => x.Duration).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasMaxLength(50);
            entity.Property(x => x.Instructions).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PrescriptionId);
            entity.HasOne(x => x.Prescription)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LabOrder>(entity =>
        {
            entity.ToTable("LabOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.ConsultationId);
            entity.Property(x => x.BookingId);
            entity.Property(x => x.RequestedByDoctorId);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Requested");
            entity.Property(x => x.RequestedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.RequestedByDoctorId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.RequestedByDoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LabOrderItem>(entity =>
        {
            entity.ToTable("LabOrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LabOrderId).IsRequired();
            entity.Property(x => x.TestName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.TestCode).HasMaxLength(50);
            entity.Property(x => x.Instructions).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.LabOrderId);
            entity.HasOne(x => x.LabOrder)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.LabOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LabResult>(entity =>
        {
            entity.ToTable("LabResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.ConsultationId);
            entity.Property(x => x.BookingId);
            entity.Property(x => x.LabOrderItemId);
            entity.Property(x => x.UploadedByUserId).HasMaxLength(450);
            entity.Property(x => x.ResultTitle).HasMaxLength(200);
            entity.Property(x => x.ResultText).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ResultFileUrl).HasMaxLength(500);
            entity.Property(x => x.FileName).HasMaxLength(500);
            entity.Property(x => x.FileContentType).HasMaxLength(100);
            entity.Property(x => x.UploadedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Uploaded");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.LabOrderItemId);
            entity.HasIndex(x => x.UploadedByUserId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LabOrderItem>()
                .WithMany()
                .HasForeignKey(x => x.LabOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConsultationFollowUp>(entity =>
        {
            entity.ToTable("ConsultationFollowUps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.ConsultationId).IsRequired();
            entity.Property(x => x.BookingId);
            entity.Property(x => x.FollowUpDate).IsRequired().HasColumnType("date");
            entity.Property(x => x.Instructions).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Reason).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.BookingId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PatientDocument>(entity =>
        {
            entity.ToTable("PatientDocuments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.BookingId);
            entity.Property(x => x.ConsultationId);
            entity.Property(x => x.UploadedByUserId).HasMaxLength(450);
            entity.Property(x => x.DocumentType).IsRequired().HasMaxLength(30).HasDefaultValue("Other");
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnType("nvarchar(max)");
            entity.Property(x => x.FileUrl).HasMaxLength(500);
            entity.Property(x => x.FileName).HasMaxLength(500);
            entity.Property(x => x.FileContentType).HasMaxLength(100);
            entity.Property(x => x.FileSize);
            entity.Property(x => x.Source).IsRequired().HasMaxLength(30).HasDefaultValue("StaffUpload");
            entity.Property(x => x.UploadedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.ConsultationId);
            entity.HasIndex(x => x.UploadedByUserId);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Consultation>()
                .WithMany()
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).IsRequired().HasMaxLength(256);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.CreatedByIp).HasMaxLength(64);
            entity.Property(x => x.ReplacedByToken).HasMaxLength(256);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExternalLoginAccount>(entity =>
        {
            entity.ToTable("ExternalLoginAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).IsRequired().HasMaxLength(20);
            entity.Property(x => x.ProviderUserId).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ProviderEmail).IsRequired().HasMaxLength(256);
            entity.Property(x => x.ProviderDisplayName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.ProviderPhotoUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PatientVaccination>(entity =>
        {
            entity.ToTable("PatientVaccinations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.BookingId);
            entity.Property(x => x.ConsultationId);
            entity.Property(x => x.DoctorId);
            entity.Property(x => x.AdministeredByUserId).HasMaxLength(450);

            // Vaccine details
            entity.Property(x => x.VaccineName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.VaccineCode).HasMaxLength(50);
            entity.Property(x => x.Manufacturer).HasMaxLength(200);
            entity.Property(x => x.LotNumber).HasMaxLength(100);
            entity.Property(x => x.ExpirationDate).HasColumnType("date");

            // Administration
            entity.Property(x => x.AdministeredDate).IsRequired().HasColumnType("date");
            entity.Property(x => x.DoseNumber).HasMaxLength(20);
            entity.Property(x => x.DoseAmount).HasPrecision(10, 2);
            entity.Property(x => x.DoseUnit).HasMaxLength(30);
            entity.Property(x => x.Route).HasMaxLength(50);
            entity.Property(x => x.Site).HasMaxLength(50);

            // Status / source
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Completed");
            entity.Property(x => x.Source).IsRequired().HasMaxLength(30).HasDefaultValue("AdministeredInClinic");

            // Follow-up
            entity.Property(x => x.NextDueDate).HasColumnType("date");

            // VIS
            entity.Property(x => x.VisEditionDate).HasColumnType("date");
            entity.Property(x => x.VisProvidedDate).HasColumnType("date");

            // Notes
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ReactionNotes).HasColumnType("nvarchar(max)");

            // Audit
            entity.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.UpdatedByUserId).HasMaxLength(450);

            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.BookingId);
            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => x.CreatedByUserId);

            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClinicSettings>(entity =>
        {
            entity.ToTable("ClinicSettings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClinicName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.Property(x => x.PrimaryColor).IsRequired().HasMaxLength(10).HasDefaultValue("#5D3E8E");
            entity.Property(x => x.SecondaryColor).IsRequired().HasMaxLength(10).HasDefaultValue("#2563EB");
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.FacebookUrl).HasMaxLength(300);
            entity.Property(x => x.InstagramUrl).HasMaxLength(300);
            entity.Property(x => x.OperatingHoursJson).IsRequired().HasMaxLength(4000).HasDefaultValue("{}");
            entity.Property(x => x.CancellationDeadlineHours).IsRequired().HasDefaultValue(24);
            entity.Property(x => x.PatientPortalEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.VaccinationReminderEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.FollowUpReminderEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.IsPayAtClinicMode).IsRequired();
            entity.Property(x => x.PayAtClinicNoShowWindowMinutes).IsRequired().HasDefaultValue(60);
            entity.Property(x => x.PrivacyPolicyText).HasMaxLength(4000);
            entity.Property(x => x.ConsentVersion).IsRequired().HasMaxLength(10).HasDefaultValue("v1.0");
            entity.Property(x => x.GcashAccountName).HasMaxLength(100);
            entity.Property(x => x.GcashNumber).HasMaxLength(20);
            entity.Property(x => x.GcashQrImageUrl).HasMaxLength(500);
            entity.Property(x => x.MayaAccountName).HasMaxLength(100);
            entity.Property(x => x.MayaNumber).HasMaxLength(20);
            entity.Property(x => x.MayaQrImageUrl).HasMaxLength(500);
            entity.Property(x => x.BankName).HasMaxLength(100);
            entity.Property(x => x.BankAccountName).HasMaxLength(100);
            entity.Property(x => x.BankAccountNumber).HasMaxLength(50);
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        // ── Notifications ────────────────────────────────
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.NavigateTo).HasMaxLength(500);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.IsRead });
        });

        // ── User Device Tokens ──────────────────────────
        builder.Entity<UserDeviceToken>(entity =>
        {
            entity.ToTable("UserDeviceTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Token).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.Platform).IsRequired().HasMaxLength(20).HasDefaultValue("web");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.Platform, x.Token }).IsUnique();
        });

        // ── Announcements ────────────────────────────────
        builder.Entity<Announcement>(entity =>
        {
            entity.ToTable("Announcements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        // ── Reviews ──────────────────────────────────────
        builder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BookingId).IsRequired();
            entity.Property(x => x.DoctorId).IsRequired();
            entity.Property(x => x.PatientId).IsRequired();
            entity.Property(x => x.Rating).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.PatientName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.DoctorId);
        });

        // ── Audit Logs ───────────────────────────────────
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(x => x.EntityId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PerformedBy).IsRequired().HasMaxLength(450);
            entity.Property(x => x.PerformedAt).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasIndex(x => x.EntityType);
            entity.HasIndex(x => x.PerformedAt);
        });
    }
}
