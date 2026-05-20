using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using ClinicApp.Domain.Entities.Clinic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Seeding;

public sealed class ClinicSeeder : IClinicSeeder
{
    private const string DoctorPassword = "Doctor@123456";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicSeeder(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.Doctors.AnyAsync(cancellationToken))
        {
            await SeedDoctorsAsync(cancellationToken);
        }

        if (!await _dbContext.Services.AnyAsync(cancellationToken))
        {
            await SeedServicesAsync(cancellationToken);
        }

        await LinkGeneralConsultationAsync(cancellationToken);
        await LinkGeneralConsultationToRandolfAsync(cancellationToken);
        await LinkGeneralConsultationToActiveDoctorsWithoutServicesAsync(cancellationToken);
    }

    private async Task SeedDoctorsAsync(CancellationToken cancellationToken)
    {
        var doctorSeeds = new[]
        {
            new DoctorSeed(
                Email: "dr.santos@gavino.clinic",
                FullName: "Dr. Miguel Santos",
                Specialization: "Internal Medicine",
                Bio: "Experienced physician focused on adult primary care and chronic disease management.",
                LicenseNumber: "LIC-DR-1001",
                PtrNumber: "PTR-1001",
                S2Number: null,
                ConsultationFee: 650m),
            new DoctorSeed(
                Email: "dr.reyes@gavino.clinic",
                FullName: "Dr. Jose Reyes",
                Specialization: "Family Medicine",
                Bio: "Provides holistic family-centered care with a strong preventive medicine background.",
                LicenseNumber: "LIC-DR-1002",
                PtrNumber: "PTR-1002",
                S2Number: null,
                ConsultationFee: 600m)
        };

        foreach (var seed in doctorSeeds)
        {
            var user = await EnsureDoctorUserAsync(seed, cancellationToken);
            var doctorExists = await _dbContext.Doctors.AnyAsync(x => x.UserId == user.Id, cancellationToken);
            if (doctorExists)
            {
                continue;
            }

            var now = DateTime.UtcNow;
            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FullName = seed.FullName,
                Specialization = seed.Specialization,
                Bio = seed.Bio,
                ProfilePhotoUrl = null,
                LicenseNumber = seed.LicenseNumber,
                PtrNumber = seed.PtrNumber,
                S2Number = seed.S2Number,
                ConsultationFee = seed.ConsultationFee,
                SlotDurationMinutes = 30,
                SlotCapacity = 1,
                DailyPatientLimit = null,
                Status = "Active",
                AverageRating = null,
                ReviewCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Doctors.Add(doctor);

            foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            {
                _dbContext.DoctorSchedules.Add(new DoctorSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctor.Id,
                    DayOfWeek = day.ToString(),
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(17, 0)
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> EnsureDoctorUserAsync(DoctorSeed seed, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(seed.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                EmailConfirmed = true,
                FullName = seed.FullName,
                Role = "Doctor",
                AuthProvider = "Local",
                IsActive = true,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, DoctorPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed doctor user {seed.Email}: {string.Join(", ", createResult.Errors.Select(x => x.Description))}");
            }
        }

        user.FullName = seed.FullName;
        user.Role = "Doctor";
        user.IsActive = true;
        user.IsFirstLogin = true;
        user.AuthProvider = "Local";
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update doctor user {seed.Email}: {string.Join(", ", updateResult.Errors.Select(x => x.Description))}");
        }

        if (!await _userManager.IsInRoleAsync(user, "Doctor"))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "Doctor");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add doctor role for {seed.Email}: {string.Join(", ", roleResult.Errors.Select(x => x.Description))}");
            }
        }

        return user;
    }

    private async Task SeedServicesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var services = new[]
        {
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "General Consultation",
                Description = "Initial and follow-up consultation with a primary care physician.",
                Category = "Consultation",
                Price = 500m,
                EstimatedDurationMinutes = 30,
                IsActive = true,
                CreatedAt = now
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "Blood Test",
                Description = "Basic laboratory blood work.",
                Category = "Laboratory",
                Price = 250m,
                EstimatedDurationMinutes = 20,
                IsActive = true,
                CreatedAt = now
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "X-Ray",
                Description = "Basic radiology imaging service.",
                Category = "Diagnostic",
                Price = 800m,
                EstimatedDurationMinutes = 20,
                IsActive = true,
                CreatedAt = now
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "Urinalysis",
                Description = "Routine urine laboratory examination.",
                Category = "Laboratory",
                Price = 180m,
                EstimatedDurationMinutes = 15,
                IsActive = true,
                CreatedAt = now
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "ECG",
                Description = "Electrocardiogram screening service.",
                Category = "Diagnostic",
                Price = 450m,
                EstimatedDurationMinutes = 20,
                IsActive = true,
                CreatedAt = now
            }
        };

        _dbContext.Services.AddRange(services);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkGeneralConsultationAsync(CancellationToken cancellationToken)
    {
        var generalConsultation = await _dbContext.Services
            .SingleOrDefaultAsync(x => x.Name == "General Consultation", cancellationToken);

        if (generalConsultation is null)
        {
            return;
        }

        var doctorIds = await _dbContext.Doctors
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Take(2)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (doctorIds.Count == 0)
        {
            return;
        }

        var existingLinks = await _dbContext.DoctorServices
            .Where(x => x.ServiceId == generalConsultation.Id && doctorIds.Contains(x.DoctorId))
            .Select(x => x.DoctorId)
            .ToListAsync(cancellationToken);

        var missingDoctorIds = doctorIds.Except(existingLinks).ToList();
        if (missingDoctorIds.Count == 0)
        {
            return;
        }

        _dbContext.DoctorServices.AddRange(missingDoctorIds.Select(doctorId => new DoctorService
        {
            DoctorId = doctorId,
            ServiceId = generalConsultation.Id
        }));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkGeneralConsultationToRandolfAsync(CancellationToken cancellationToken)
    {
        var generalConsultation = await _dbContext.Services
            .SingleOrDefaultAsync(x => x.Name == "General Consultation", cancellationToken);

        if (generalConsultation is null)
        {
            return;
        }

        var doctors = await _dbContext.Doctors
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .ToListAsync(cancellationToken);

        var randolfDoctors = doctors
            .Where(x =>
                x.FullName.Contains("randolf", StringComparison.OrdinalIgnoreCase) ||
                x.FullName.Contains("butantan1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (randolfDoctors.Count == 0)
        {
            return;
        }

        var existingLinks = await _dbContext.DoctorServices
            .Where(x => x.ServiceId == generalConsultation.Id && randolfDoctors.Select(d => d.Id).Contains(x.DoctorId))
            .Select(x => x.DoctorId)
            .ToListAsync(cancellationToken);

        var missingDoctorIds = randolfDoctors
            .Select(x => x.Id)
            .Except(existingLinks)
            .ToList();

        if (missingDoctorIds.Count == 0)
        {
            return;
        }

        _dbContext.DoctorServices.AddRange(missingDoctorIds.Select(doctorId => new DoctorService
        {
            DoctorId = doctorId,
            ServiceId = generalConsultation.Id
        }));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkGeneralConsultationToActiveDoctorsWithoutServicesAsync(CancellationToken cancellationToken)
    {
        var generalConsultation = await _dbContext.Services
            .SingleOrDefaultAsync(x => x.Name == "General Consultation", cancellationToken);

        if (generalConsultation is null)
        {
            return;
        }

        var activeDoctorIds = await _dbContext.Doctors
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (activeDoctorIds.Count == 0)
        {
            return;
        }

        var linkedDoctorIds = await _dbContext.DoctorServices
            .AsNoTracking()
            .Where(x => activeDoctorIds.Contains(x.DoctorId))
            .Select(x => x.DoctorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingDoctorIds = activeDoctorIds
            .Except(linkedDoctorIds)
            .ToList();

        if (missingDoctorIds.Count == 0)
        {
            return;
        }

        _dbContext.DoctorServices.AddRange(missingDoctorIds.Select(doctorId => new DoctorService
        {
            DoctorId = doctorId,
            ServiceId = generalConsultation.Id
        }));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DoctorSeed(
        string Email,
        string FullName,
        string Specialization,
        string? Bio,
        string? LicenseNumber,
        string? PtrNumber,
        string? S2Number,
        decimal ConsultationFee);
}
