namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Review
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
