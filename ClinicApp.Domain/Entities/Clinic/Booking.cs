namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Booking
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ServiceId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly SlotStartTime { get; set; }
    public TimeOnly SlotEndTime { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string PaymentMode { get; set; } = "Online";
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
    public string? CheckedInByUserId { get; set; }
    public DateTime? DoctorCompletedAt { get; set; }
    public string? DoctorCompletedByUserId { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? Diagnosis { get; set; }
    public string? DoctorFeeNotes { get; set; }
    public string? SoapNotes { get; set; }
    public string? PrescriptionJson { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpInstructions { get; set; }
    public bool IsProfessionalFeeWaived { get; set; }
    public string? ProfessionalFeeWaivedReason { get; set; }
    public string? ProfessionalFeeWaivedByUserId { get; set; }
    public DateTime? ProfessionalFeeWaivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public Service? Service { get; set; }
    public Payment? Payment { get; set; }
    public Booking? RescheduledFromBooking { get; set; }
    public ICollection<BookingServiceItem> BookingServiceItems { get; set; } = new List<BookingServiceItem>();
}
