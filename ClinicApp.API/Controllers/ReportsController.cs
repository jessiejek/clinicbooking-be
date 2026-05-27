using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("unpaid-completed-visits")]
    public async Task<ActionResult<List<UnpaidCompletedVisitRow>>> GetUnpaidCompletedVisits(CancellationToken ct)
    {
        var rows = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Doctor)
            .Include(b => b.BookingServiceItems)
            .Where(b => b.Status == "Completed" && b.PaymentStatus != "Paid")
            .OrderByDescending(b => b.AppointmentDate)
            .Select(b => new UnpaidCompletedVisitRow
            {
                Patient = b.Patient != null ? $"{b.Patient.FirstName} {b.Patient.LastName}" : "Unknown",
                Doctor = b.Doctor != null ? b.Doctor.FullName : "Unknown",
                Service = b.BookingServiceItems.Select(s => s.Name).FirstOrDefault() ?? "",
                VisitDate = b.AppointmentDate.ToString(),
                Amount = b.FinalAmount ?? 0,
                PaymentStatus = b.PaymentStatus ?? "Unpaid"
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("pending-follow-ups")]
    public async Task<ActionResult<List<PendingFollowUpRow>>> GetPendingFollowUps(CancellationToken ct)
    {
        var rows = await _db.ConsultationFollowUps
            .AsNoTracking()
            .Include(f => f.Consultation)
                .ThenInclude(c => c.Booking)
            .Where(f => f.Status == "Pending")
            .OrderBy(f => f.FollowUpDate)
            .Select(f => new PendingFollowUpRow
            {
                Patient = f.Consultation != null && f.Consultation.Booking != null && f.Consultation.Booking.Patient != null
                    ? f.Consultation.Booking.Patient.FirstName + " " + f.Consultation.Booking.Patient.LastName
                    : "Unknown",
                Doctor = f.Consultation != null && f.Consultation.Booking != null && f.Consultation.Booking.Doctor != null
                    ? f.Consultation.Booking.Doctor.FullName
                    : "Unknown",
                FollowUpDate = f.FollowUpDate.ToString(),
                Reason = f.Reason ?? "",
                Status = f.Status
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("daily-booking-summary")]
    public async Task<ActionResult<List<DailyBookingSummaryRow>>> GetDailyBookingSummary(CancellationToken ct)
    {
        var rows = await _db.Bookings
            .AsNoTracking()
            .GroupBy(b => b.AppointmentDate)
            .OrderByDescending(g => g.Key)
            .Select(g => new DailyBookingSummaryRow
            {
                Date = g.Key.ToString(),
                TotalBookings = g.Count(),
                Completed = g.Count(b => b.Status == "Completed"),
                Cancelled = g.Count(b => b.Status == "Cancelled"),
                NoShow = g.Count(b => b.Status == "NoShow"),
                Revenue = g.Where(b => b.Status == "Completed" && b.PaymentStatus == "Paid")
                    .Sum(b => b.FinalAmount ?? 0)
            })
            .Take(90)
            .ToListAsync(ct);

        return Ok(rows);
    }
}

public sealed class UnpaidCompletedVisitRow
{
    public string Patient { get; set; } = string.Empty;
    public string Doctor { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string VisitDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public sealed class PendingFollowUpRow
{
    public string Patient { get; set; } = string.Empty;
    public string Doctor { get; set; } = string.Empty;
    public string FollowUpDate { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class DailyBookingSummaryRow
{
    public string Date { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public decimal Revenue { get; set; }
}
