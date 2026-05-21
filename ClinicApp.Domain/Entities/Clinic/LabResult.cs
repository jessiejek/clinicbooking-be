namespace ClinicApp.Domain.Entities.Clinic;

public sealed class LabResult
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? LabOrderItemId { get; set; }
    public string? UploadedByUserId { get; set; }
    public string? ResultTitle { get; set; }
    public string? ResultText { get; set; }
    public string? ResultFileUrl { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = "Uploaded";
    public DateTime CreatedAt { get; set; }
}
