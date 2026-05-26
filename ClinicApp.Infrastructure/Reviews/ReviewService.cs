using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReviewResponseDto>> GetDoctorReviewsAsync(Guid doctorId, CancellationToken ct = default)
    {
        return await _db.Reviews
            .AsNoTracking()
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id.ToString(),
                BookingId = r.BookingId.ToString(),
                DoctorId = r.DoctorId.ToString(),
                PatientId = r.PatientId.ToString(),
                Rating = r.Rating,
                Comment = r.Comment,
                PatientName = r.PatientName,
                CreatedAt = r.CreatedAt.ToString("o")
            })
            .ToListAsync(ct);
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(Guid doctorId, Guid patientId, Guid bookingId, int rating, string? comment, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstAsync(p => p.Id == patientId, ct);
        var patientName = $"{patient.FirstName} {patient.LastName}";

        var entity = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            DoctorId = doctorId,
            PatientId = patientId,
            Rating = rating,
            Comment = comment,
            PatientName = patientName,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ReviewResponseDto
        {
            Id = entity.Id.ToString(),
            BookingId = entity.BookingId.ToString(),
            DoctorId = entity.DoctorId.ToString(),
            PatientId = entity.PatientId.ToString(),
            Rating = entity.Rating,
            Comment = entity.Comment,
            PatientName = entity.PatientName,
            CreatedAt = entity.CreatedAt.ToString("o")
        };
    }
}
