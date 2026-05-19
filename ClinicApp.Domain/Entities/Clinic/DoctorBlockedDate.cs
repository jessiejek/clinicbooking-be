namespace ClinicApp.Domain.Entities.Clinic;

public sealed class DoctorBlockedDate
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly BlockedDate { get; set; }
    public string? Reason { get; set; }
}
