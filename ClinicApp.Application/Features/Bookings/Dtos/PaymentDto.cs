namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string? ProofImageUrl,
    string Status,
    string? OrNumber,
    string? VerifiedByUserId,
    DateTime? VerifiedAt,
    string? WaivedByUserId,
    DateTime? WaivedAt,
    string? WaivedReason,
    string? RefundedByUserId,
    DateTime? RefundedAt,
    string? RefundReason,
    DateTime CreatedAt);
