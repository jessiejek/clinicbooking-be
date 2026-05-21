namespace ClinicApp.Domain.Entities.Clinic;

public sealed class LabOrder
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? RequestedByDoctorId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Requested";
    public DateTime RequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
