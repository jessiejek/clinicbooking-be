namespace ClinicApp.Domain.Entities.Clinic;

public sealed class DoctorSchedule
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
