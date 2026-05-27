namespace ClinicApp.Domain.Entities.Clinic;

public sealed class PatientAllergy
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty;
    public string Severity { get; set; } = "Mild";
    public string? AllergenType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
