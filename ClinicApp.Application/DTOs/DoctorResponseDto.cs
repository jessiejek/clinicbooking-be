namespace ClinicApp.Application.DTOs;

public sealed class DoctorResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? LicenseNumber { get; set; }
    public string? PtrNumber { get; set; }
    public string? S2Number { get; set; }
    public decimal ConsultationFee { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int SlotCapacity { get; set; }
    public int? DailyPatientLimit { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
    public string[]? WorkingDays { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
