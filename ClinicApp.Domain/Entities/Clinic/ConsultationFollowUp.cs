namespace ClinicApp.Domain.Entities.Clinic;

public sealed class ConsultationFollowUp
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid? BookingId { get; set; }
    public DateOnly FollowUpDate { get; set; }
    public string? Instructions { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
