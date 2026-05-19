using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Settings.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Settings;

public sealed class ClinicSettingsService : IClinicSettingsService
{
    private const string DefaultClinicName = "Dr. Grace E. Gavino Medical Clinic";

    private readonly AppDbContext _dbContext;

    public ClinicSettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClinicSettingsDto> GetAsync(CancellationToken ct)
    {
        var settings = await _dbContext.ClinicSettings.SingleOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = CreateDefaultSettings();
            _dbContext.ClinicSettings.Add(settings);
            await _dbContext.SaveChangesAsync(ct);
        }

        return Map(settings);
    }

    public async Task<ClinicSettingsDto> UpdateAsync(UpdateClinicSettingsDto dto, CancellationToken ct)
    {
        var settings = await _dbContext.ClinicSettings.SingleOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = CreateDefaultSettings();
            _dbContext.ClinicSettings.Add(settings);
            await _dbContext.SaveChangesAsync(ct);
        }

        ApplyUpdates(settings, dto);
        settings.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return Map(settings);
    }

    private static ClinicSettings CreateDefaultSettings()
    {
        return new ClinicSettings
        {
            Id = Guid.NewGuid(),
            ClinicName = DefaultClinicName,
            PrimaryColor = "#5D3E8E",
            SecondaryColor = "#2563EB",
            OperatingHoursJson = "{}",
            CancellationDeadlineHours = 24,
            PatientPortalEnabled = true,
            VaccinationReminderEnabled = true,
            FollowUpReminderEnabled = true,
            PayAtClinicNoShowWindowMinutes = 60,
            ConsentVersion = "v1.0",
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static void ApplyUpdates(ClinicSettings settings, UpdateClinicSettingsDto dto)
    {
        if (dto.ClinicName is not null)
        {
            settings.ClinicName = dto.ClinicName;
        }

        if (dto.LogoUrl is not null)
        {
            settings.LogoUrl = dto.LogoUrl;
        }

        if (dto.PrimaryColor is not null)
        {
            settings.PrimaryColor = dto.PrimaryColor;
        }

        if (dto.SecondaryColor is not null)
        {
            settings.SecondaryColor = dto.SecondaryColor;
        }

        if (dto.Address is not null)
        {
            settings.Address = dto.Address;
        }

        if (dto.Phone is not null)
        {
            settings.Phone = dto.Phone;
        }

        if (dto.ContactEmail is not null)
        {
            settings.ContactEmail = dto.ContactEmail;
        }

        if (dto.FacebookUrl is not null)
        {
            settings.FacebookUrl = dto.FacebookUrl;
        }

        if (dto.InstagramUrl is not null)
        {
            settings.InstagramUrl = dto.InstagramUrl;
        }

        if (dto.OperatingHoursJson is not null)
        {
            settings.OperatingHoursJson = dto.OperatingHoursJson;
        }

        if (dto.CancellationDeadlineHours.HasValue)
        {
            settings.CancellationDeadlineHours = dto.CancellationDeadlineHours.Value;
        }

        if (dto.PatientPortalEnabled.HasValue)
        {
            settings.PatientPortalEnabled = dto.PatientPortalEnabled.Value;
        }

        if (dto.VaccinationReminderEnabled.HasValue)
        {
            settings.VaccinationReminderEnabled = dto.VaccinationReminderEnabled.Value;
        }

        if (dto.FollowUpReminderEnabled.HasValue)
        {
            settings.FollowUpReminderEnabled = dto.FollowUpReminderEnabled.Value;
        }

        if (dto.IsPayAtClinicMode.HasValue)
        {
            settings.IsPayAtClinicMode = dto.IsPayAtClinicMode.Value;
        }

        if (dto.PayAtClinicNoShowWindowMinutes.HasValue)
        {
            settings.PayAtClinicNoShowWindowMinutes = dto.PayAtClinicNoShowWindowMinutes.Value;
        }

        if (dto.PrivacyPolicyText is not null)
        {
            settings.PrivacyPolicyText = dto.PrivacyPolicyText;
        }

        if (dto.ConsentVersion is not null)
        {
            settings.ConsentVersion = dto.ConsentVersion;
        }

        if (dto.GcashAccountName is not null)
        {
            settings.GcashAccountName = dto.GcashAccountName;
        }

        if (dto.GcashNumber is not null)
        {
            settings.GcashNumber = dto.GcashNumber;
        }

        if (dto.GcashQrImageUrl is not null)
        {
            settings.GcashQrImageUrl = dto.GcashQrImageUrl;
        }

        if (dto.MayaAccountName is not null)
        {
            settings.MayaAccountName = dto.MayaAccountName;
        }

        if (dto.MayaNumber is not null)
        {
            settings.MayaNumber = dto.MayaNumber;
        }

        if (dto.MayaQrImageUrl is not null)
        {
            settings.MayaQrImageUrl = dto.MayaQrImageUrl;
        }

        if (dto.BankName is not null)
        {
            settings.BankName = dto.BankName;
        }

        if (dto.BankAccountName is not null)
        {
            settings.BankAccountName = dto.BankAccountName;
        }

        if (dto.BankAccountNumber is not null)
        {
            settings.BankAccountNumber = dto.BankAccountNumber;
        }
    }

    private static ClinicSettingsDto Map(ClinicSettings settings)
    {
        return new ClinicSettingsDto(
            Id: settings.Id,
            ClinicName: settings.ClinicName,
            LogoUrl: settings.LogoUrl,
            PrimaryColor: settings.PrimaryColor,
            SecondaryColor: settings.SecondaryColor,
            Address: settings.Address,
            Phone: settings.Phone,
            ContactEmail: settings.ContactEmail,
            FacebookUrl: settings.FacebookUrl,
            InstagramUrl: settings.InstagramUrl,
            OperatingHoursJson: settings.OperatingHoursJson,
            CancellationDeadlineHours: settings.CancellationDeadlineHours,
            PatientPortalEnabled: settings.PatientPortalEnabled,
            VaccinationReminderEnabled: settings.VaccinationReminderEnabled,
            FollowUpReminderEnabled: settings.FollowUpReminderEnabled,
            IsPayAtClinicMode: settings.IsPayAtClinicMode,
            PayAtClinicNoShowWindowMinutes: settings.PayAtClinicNoShowWindowMinutes,
            PrivacyPolicyText: settings.PrivacyPolicyText,
            ConsentVersion: settings.ConsentVersion,
            GcashAccountName: settings.GcashAccountName,
            GcashNumber: settings.GcashNumber,
            GcashQrImageUrl: settings.GcashQrImageUrl,
            MayaAccountName: settings.MayaAccountName,
            MayaNumber: settings.MayaNumber,
            MayaQrImageUrl: settings.MayaQrImageUrl,
            BankName: settings.BankName,
            BankAccountName: settings.BankAccountName,
            BankAccountNumber: settings.BankAccountNumber,
            UpdatedAt: settings.UpdatedAt);
    }
}
