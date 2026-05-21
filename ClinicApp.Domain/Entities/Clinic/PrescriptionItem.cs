namespace ClinicApp.Domain.Entities.Clinic;

public sealed class PrescriptionItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Dosage { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Quantity { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
}
