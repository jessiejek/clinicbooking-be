namespace ClinicApp.Domain.Entities.Clinic;

public sealed class ClinicSettings
{
    public Guid Id { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#5D3E8E";
    public string SecondaryColor { get; set; } = "#2563EB";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? ContactEmail { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string OperatingHoursJson { get; set; } = "{}";
    public int CancellationDeadlineHours { get; set; } = 24;
    public bool PatientPortalEnabled { get; set; } = true;
    public bool VaccinationReminderEnabled { get; set; } = true;
    public bool FollowUpReminderEnabled { get; set; } = true;
    public bool IsPayAtClinicMode { get; set; }
    public int PayAtClinicNoShowWindowMinutes { get; set; } = 60;
    public string? PrivacyPolicyText { get; set; }
    public string ConsentVersion { get; set; } = "v1.0";
    public string? GcashAccountName { get; set; }
    public string? GcashNumber { get; set; }
    public string? GcashQrImageUrl { get; set; }
    public string? MayaAccountName { get; set; }
    public string? MayaNumber { get; set; }
    public string? MayaQrImageUrl { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public DateTime UpdatedAt { get; set; }
}
