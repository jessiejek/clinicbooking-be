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
                Service = b.BookingServiceItems.Select(s => s.ServiceNameSnapshot).FirstOrDefault() ?? "",
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
            .Where(f => f.Status == "Pending")
            .OrderBy(f => f.FollowUpDate)
            .ToListAsync(ct);

        var patientIds = rows.Where(f => f.PatientId != Guid.Empty).Select(f => f.PatientId).Distinct().ToList();
        var bookingIds = rows.Where(f => f.BookingId.HasValue).Select(f => f.BookingId!.Value).Distinct().ToList();

        var patients = await _db.Patients.AsNoTracking()
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => bookingIds.Contains(b.Id))
            .ToListAsync(ct);

        var doctors = await _db.Doctors.AsNoTracking()
            .Where(d => bookings.Select(b => b.DoctorId).Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, ct);

        var result = rows.Select(f =>
        {
            var patient = f.PatientId != Guid.Empty && patients.TryGetValue(f.PatientId, out var p) ? p : null;
            var booking = f.BookingId.HasValue ? bookings.FirstOrDefault(b => b.Id == f.BookingId.Value) : null;
            var doctor = booking is not null && doctors.TryGetValue(booking.DoctorId, out var d) ? d : null;

            return new PendingFollowUpRow
            {
                Patient = patient is not null ? $"{patient.FirstName} {patient.LastName}" : "Unknown",
                Doctor = doctor?.FullName ?? "Unknown",
                FollowUpDate = f.FollowUpDate.ToString(),
                Reason = f.Reason ?? "",
                Status = f.Status
            };
        }).ToList();

        return Ok(result);
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
