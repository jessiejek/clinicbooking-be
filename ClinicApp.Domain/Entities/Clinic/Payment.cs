namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? ProofImageUrl { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string? OrNumber { get; set; }
    public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? WaivedByUserId { get; set; }
    public DateTime? WaivedAt { get; set; }
    public string? WaivedReason { get; set; }
    public string? RefundedByUserId { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Booking? Booking { get; set; }
}
