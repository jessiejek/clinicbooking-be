namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Consultation
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? BookingId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? ChiefComplaint { get; set; }
    public string? HistoryOfPresentIllness { get; set; }
    public string? PeGeneralFindings { get; set; }
    public TimeOnly? ConsultationTime { get; set; }
    public bool IsLocked { get; set; }
    public string? GeneralNotes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
