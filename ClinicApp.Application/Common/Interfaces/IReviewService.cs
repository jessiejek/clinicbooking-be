using ClinicApp.Application.DTOs;

namespace ClinicApp.Application.Common.Interfaces;

public interface IReviewService
{
    Task<List<ReviewResponseDto>> GetDoctorReviewsAsync(Guid doctorId, CancellationToken ct = default);
    Task<ReviewResponseDto> CreateReviewAsync(Guid doctorId, Guid patientId, Guid bookingId, int rating, string? comment, CancellationToken ct = default);
}
