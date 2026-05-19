namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Doctor
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? LicenseNumber { get; set; }
    public string? PtrNumber { get; set; }
    public string? S2Number { get; set; }
    public decimal ConsultationFee { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public int SlotCapacity { get; set; } = 1;
    public int? DailyPatientLimit { get; set; }
    public string Status { get; set; } = "Active";
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
}
