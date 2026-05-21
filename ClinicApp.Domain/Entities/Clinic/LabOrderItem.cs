namespace ClinicApp.Domain.Entities.Clinic;

public sealed class LabOrderItem
{
    public Guid Id { get; set; }
    public Guid LabOrderId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string? Instructions { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}
