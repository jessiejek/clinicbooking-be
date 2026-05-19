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
