using System.Globalization;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Seeding;

public sealed class PatientSeeder : IPatientSeeder
{
    private const string PatientEmail = "patient@gavino.clinic";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientSeeder(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(PatientEmail);
        if (user is null)
        {
            throw new InvalidOperationException($"Unable to seed patient profile because {PatientEmail} was not found.");
        }

        if (await _dbContext.Patients.AnyAsync(x => x.UserId == user.Id, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            PatientCode = await GeneratePatientCodeAsync(now.Year, cancellationToken),
            UserId = user.Id,
            FirstName = "Juan",
            MiddleName = null,
            LastName = "Dela Cruz",
            DateOfBirth = new DateOnly(1995, 5, 12),
            Sex = "Male",
            CivilStatus = "Single",
            Address = "123 Sample Street",
            City = "Manila",
            ZipCode = "1000",
            ContactNumber = "09171234567",
            Email = PatientEmail,
            EmergencyContactName = "Maria Dela Cruz",
            EmergencyContactNumber = "09170000000",
            EmergencyContactRelationship = "Mother",
            BloodType = "O+",
            PhilHealthNumber = null,
            HmoProvider = null,
            HmoCardNumber = null,
            IsGuest = false,
            IsEmailVerified = false,
            ConsentedAt = null,
            ConsentVersion = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GeneratePatientCodeAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"PT-{year}-";
        var latestCode = await _dbContext.Patients.AsNoTracking()
            .Where(x => x.PatientCode.StartsWith(prefix))
            .OrderByDescending(x => x.PatientCode)
            .Select(x => x.PatientCode)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (!string.IsNullOrWhiteSpace(latestCode) && latestCode.Length >= prefix.Length + 5)
        {
            var sequencePart = latestCode[prefix.Length..];
            if (int.TryParse(sequencePart, NumberStyles.None, CultureInfo.InvariantCulture, out var currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D5}";
    }
}
