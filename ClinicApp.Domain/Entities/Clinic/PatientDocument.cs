namespace ClinicApp.Domain.Entities.Clinic;

public sealed class PatientDocument
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? UploadedByUserId { get; set; }
    public string DocumentType { get; set; } = "Other";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
    public long? FileSize { get; set; }
    public string Source { get; set; } = "StaffUpload";
    public DateTime UploadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
