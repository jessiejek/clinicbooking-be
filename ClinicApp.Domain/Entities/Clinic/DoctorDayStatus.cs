namespace ClinicApp.Domain.Entities.Clinic;

public sealed class DoctorDayStatus
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? RunningLateMinutes { get; set; }
}
