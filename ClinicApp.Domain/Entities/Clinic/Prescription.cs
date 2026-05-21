namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Prescription
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? DoctorId { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
