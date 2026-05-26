namespace ClinicApp.Application.DTOs;

public sealed class BookingResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public List<string>? ServiceIds { get; set; }
    public string? ServiceName { get; set; }
    public List<string>? ServiceNames { get; set; }
    public List<BookingServiceItemDto>? Services { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string SlotStartTime { get; set; } = string.Empty;
    public string SlotEndTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public int? QueueNumber { get; set; }
    public decimal TotalFee { get; set; }
    public decimal? FinalAmount { get; set; }
    public decimal? AmountDue { get; set; }
    public decimal ConsultationFeeSnapshot { get; set; }
    public decimal ServiceFeeSnapshot { get; set; }
    public bool IsWalkIn { get; set; }
    public string? ProofType { get; set; }
    public string? ProofValue { get; set; }
    public string? ProofSubmittedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public string? RescheduledFromBookingId { get; set; }
    public string? ReceiptUrl { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? OrNumber { get; set; }
    public string? CheckedInAt { get; set; }
    public string? DoctorCompletedAt { get; set; }
    public bool IsProfessionalFeeWaived { get; set; }
    public string? ProfessionalFeeWaivedReason { get; set; }

    // Nested objects
    public BookingPatientDto? Patient { get; set; }
    public BookingDoctorDto? Doctor { get; set; }
    public BookingServiceDto? Service { get; set; }
    public PaymentResponseDto? Payment { get; set; }
}

public sealed class BookingPatientDto
{
    public string Id { get; set; } = string.Empty;
    public string? PatientCode { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public bool? IsGuest { get; set; }
}

public sealed class BookingDoctorDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? FullName { get; set; }
    public string? Specialization { get; set; }
    public decimal? ConsultationFee { get; set; }
    public string? Status { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public sealed class BookingServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class BookingServiceItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? Price { get; set; }
}

public sealed class PaymentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? ProofImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OrNumber { get; set; }
    public string? VerifiedByUserId { get; set; }
    public string? VerifiedAt { get; set; }
    public string? VerifiedByName { get; set; }
    public string? CashierName { get; set; }
    public string? PaidAt { get; set; }
    public string? WaivedByUserId { get; set; }
    public string? WaivedAt { get; set; }
    public string? WaivedByName { get; set; }
    public string? WaivedReason { get; set; }
    public string? RefundedByUserId { get; set; }
    public string? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
}
