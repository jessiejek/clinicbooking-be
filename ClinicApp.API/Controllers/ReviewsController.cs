using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly AppDbContext _db;

    public ReviewsController(IReviewService reviewService, AppDbContext db)
    {
        _reviewService = reviewService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetReviews([FromQuery] Guid doctorId, CancellationToken ct)
    {
        if (doctorId == Guid.Empty)
            return BadRequest("doctorId is required");

        var reviews = await _reviewService.GetDoctorReviewsAsync(doctorId, ct);
        return Ok(reviews);
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (patient is null)
            return BadRequest("Patient profile not found");

        var alreadyReviewed = await _db.Reviews.AsNoTracking()
            .AnyAsync(r => r.BookingId == request.BookingId && r.PatientId == patient.Id, ct);
        if (alreadyReviewed)
            return BadRequest("This booking has already been reviewed.");

        var review = await _reviewService.CreateReviewAsync(request.DoctorId, patient.Id, request.BookingId, request.Rating, request.Comment, ct);
        return Ok(review);
    }
}

public sealed class CreateReviewRequest
{
    public Guid DoctorId { get; set; }
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
