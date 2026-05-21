namespace ClinicApp.Domain.Entities.Clinic;

public sealed class ConsultationSoapNote
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ConsultationId { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
