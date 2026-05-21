namespace ClinicApp.Domain.Entities.Clinic;

public sealed class BookingServiceItem
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceNameSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Booking? Booking { get; set; }
    public Service? Service { get; set; }
}
