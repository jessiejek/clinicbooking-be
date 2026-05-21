namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Consultation
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? BookingId { get; set; }
    public string Status { get; set; } = "Open";
    public string? GeneralNotes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
