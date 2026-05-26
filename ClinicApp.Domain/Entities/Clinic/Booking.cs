namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Booking
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    [Obsolete("Will be removed — services are via BookingServiceItems")]
    public Guid ServiceId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly SlotStartTime { get; set; }
    public TimeOnly SlotEndTime { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string PaymentMode { get; set; } = "PayAtClinic";
    public int? QueueNumber { get; set; }
    public decimal TotalFee { get; set; }
    public decimal ConsultationFeeSnapshot { get; set; }
    public decimal ServiceFeeSnapshot { get; set; }
    public bool IsWalkIn { get; set; }
    public string? ProofType { get; set; }
    public string? ProofValue { get; set; }
    public DateTime? ProofSubmittedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public Guid? RescheduledFromBookingId { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? OrNumber { get; set; }
    public DateTime? CheckedInAt { get; set; }
    [Obsolete("Will be removed — not used by frontend")]
    public string? CheckedInByUserId { get; set; }
    public DateTime? DoctorCompletedAt { get; set; }
    [Obsolete("Will be removed — not used by frontend")]
    public string? DoctorCompletedByUserId { get; set; }
    public decimal? FinalAmount { get; set; }
    public decimal? AmountDue { get; set; }
    [Obsolete("Moved to Consultation")]
    public string? Diagnosis { get; set; }
    [Obsolete("Moved to Consultation")]
    public string? DoctorFeeNotes { get; set; }
    [Obsolete("Moved to ConsultationSoapNote")]
    public string? SoapNotes { get; set; }
    [Obsolete("Moved to Prescription")]
    public string? PrescriptionJson { get; set; }
    [Obsolete("Moved to ConsultationFollowUp")]
    public DateOnly? FollowUpDate { get; set; }
    [Obsolete("Moved to ConsultationFollowUp")]
    public string? FollowUpInstructions { get; set; }
    public bool IsProfessionalFeeWaived { get; set; }
    public string? ProfessionalFeeWaivedReason { get; set; }
    public string? ProfessionalFeeWaivedByUserId { get; set; }
    [Obsolete("Will be removed — not used by frontend")]
    public DateTime? ProfessionalFeeWaivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    [Obsolete("Will be removed — services are via BookingServiceItems")]
    public Service? Service { get; set; }
    public Payment? Payment { get; set; }
    public Booking? RescheduledFromBooking { get; set; }
    public ICollection<BookingServiceItem> BookingServiceItems { get; set; } = new List<BookingServiceItem>();
}
