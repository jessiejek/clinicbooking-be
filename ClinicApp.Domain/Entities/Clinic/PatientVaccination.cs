namespace ClinicApp.Domain.Entities.Clinic;

public sealed class PatientVaccination
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ConsultationId { get; set; }
    public Guid? DoctorId { get; set; }
    public string? AdministeredByUserId { get; set; }

    // Vaccine details
    public string VaccineName { get; set; } = string.Empty;
    public string? VaccineCode { get; set; }
    public string? Manufacturer { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    // Administration
    public DateOnly AdministeredDate { get; set; }
    public string? DoseNumber { get; set; }
    public decimal? DoseAmount { get; set; }
    public string? DoseUnit { get; set; }
    public string? Route { get; set; }
    public string? Site { get; set; }

    // Status / source
    public string Status { get; set; } = "Completed";
    public string Source { get; set; } = "AdministeredInClinic";

    // Follow-up
    public DateOnly? NextDueDate { get; set; }

    // VIS documentation
    public DateOnly? VisEditionDate { get; set; }
    public DateOnly? VisProvidedDate { get; set; }

    // Notes
    public string? Notes { get; set; }
    public string? ReactionNotes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? UpdatedByUserId { get; set; }
}
