namespace ClinicApp.Domain.Entities.Clinic;

public sealed class ConsultationDiagnosis
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ConsultationId { get; set; }
    public string DiagnosisText { get; set; } = string.Empty;
    public string? DiagnosisCode { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
