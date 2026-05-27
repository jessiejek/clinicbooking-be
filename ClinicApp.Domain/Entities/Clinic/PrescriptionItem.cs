namespace ClinicApp.Domain.Entities.Clinic;

public sealed class PrescriptionItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
    public string? Sig { get; set; }
    public int Quantity { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Route { get; set; }
    public string? Instructions { get; set; }
    public bool IsControlledSubstance { get; set; }
    public string? BrandName { get; set; }
    public DateTime CreatedAt { get; set; }
}
