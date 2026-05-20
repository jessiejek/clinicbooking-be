namespace ClinicApp.Domain.Entities.Clinic;

public sealed class Patient
{
    public Guid Id { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string? CivilStatus { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? BloodType { get; set; }
    public string? PhilHealthNumber { get; set; }
    public string? HmoProvider { get; set; }
    public string? HmoCardNumber { get; set; }
    public bool IsGuest { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? ConsentedAt { get; set; }
    public string? ConsentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
