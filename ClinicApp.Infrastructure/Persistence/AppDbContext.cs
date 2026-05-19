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
