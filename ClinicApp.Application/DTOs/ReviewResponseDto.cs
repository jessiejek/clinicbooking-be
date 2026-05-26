namespace ClinicApp.Application.DTOs;

public sealed class ReviewResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
