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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLoginAccount> ExternalLoginAccounts => Set<ExternalLoginAccount>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

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
    }
}
