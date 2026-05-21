namespace ClinicApp.Domain.Entities.Clinic;

public sealed class ConsultationVitalSign
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public Guid? BookingId { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? Temperature { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Bmi { get; set; }
    public int? PainScore { get; set; }
    public DateTime TakenAt { get; set; }
    public string? TakenByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
