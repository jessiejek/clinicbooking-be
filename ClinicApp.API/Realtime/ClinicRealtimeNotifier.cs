using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.API.Realtime;

public sealed class ClinicRealtimeNotifier : IClinicRealtimeNotifier
{
    private static readonly string[] PatientRelevantStatuses =
    {
        "Pending",
        "ProofSubmitted",
        "Confirmed",
        "CheckedIn",
        "OnHold",
        "Completed"
    };

    private readonly IHubContext<ClinicDashboardHub> _hubContext;
    private readonly AppDbContext _dbContext;

    public ClinicRealtimeNotifier(IHubContext<ClinicDashboardHub> hubContext, AppDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    public Task NotifyBookingEventAsync(
        string eventName,
        Guid bookingId,
        Guid patientId,
        Guid doctorId,
        string status,
        string paymentStatus,
        decimal? finalAmount,
        bool isProfessionalFeeWaived,
        CancellationToken cancellationToken)
    {
        var payload = new ClinicDashboardEventDto(
            EventName: eventName,
            BookingId: bookingId,
            PatientId: patientId,
            DoctorId: doctorId,
            Status: status,
            PaymentStatus: paymentStatus,
            FinalAmount: finalAmount,
            IsProfessionalFeeWaived: isProfessionalFeeWaived,
            Timestamp: DateTime.UtcNow);

        var groups = new[]
        {
            "Admin",
            "Staff",
            $"Doctor:{doctorId:D}",
            $"Patient:{patientId:D}"
        };

        return SendAsync(eventName, groups, payload, cancellationToken);
    }

    public Task NotifyDoctorScheduleUpdatedAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var payload = new ClinicDashboardEventDto(
            EventName: "DoctorScheduleUpdated",
            BookingId: null,
            PatientId: null,
            DoctorId: doctorId,
            Status: null,
            PaymentStatus: null,
            FinalAmount: null,
            IsProfessionalFeeWaived: false,
            Timestamp: DateTime.UtcNow);

        return SendAsync(
            "DoctorScheduleUpdated",
            ["Admin", "Staff", $"Doctor:{doctorId:D}"],
            payload,
            cancellationToken);
    }

    public async Task NotifyDoctorServicesUpdatedAsync(IEnumerable<Guid> doctorIds, CancellationToken cancellationToken)
    {
        foreach (var doctorId in doctorIds.Distinct())
        {
            var payload = new ClinicDashboardEventDto(
                EventName: "DoctorServicesUpdated",
                BookingId: null,
                PatientId: null,
                DoctorId: doctorId,
                Status: null,
                PaymentStatus: null,
                FinalAmount: null,
                IsProfessionalFeeWaived: false,
                Timestamp: DateTime.UtcNow);

            await SendAsync(
                "DoctorServicesUpdated",
                ["Admin", "Staff", $"Doctor:{doctorId:D}"],
                payload,
                cancellationToken);
        }
    }

    public async Task NotifyPatientProfileUpdatedAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var relatedDoctorIds = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.PatientId == patientId && PatientRelevantStatuses.Contains(x.Status))
            .Select(x => x.DoctorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var groups = new List<string>
        {
            "Admin",
            "Staff",
            $"Patient:{patientId:D}"
        };

        groups.AddRange(relatedDoctorIds.Select(doctorId => $"Doctor:{doctorId:D}"));

        var payload = new ClinicDashboardEventDto(
            EventName: "PatientProfileUpdated",
            BookingId: null,
            PatientId: patientId,
            DoctorId: null,
            Status: null,
            PaymentStatus: null,
            FinalAmount: null,
            IsProfessionalFeeWaived: false,
            Timestamp: DateTime.UtcNow);

        await SendAsync("PatientProfileUpdated", groups, payload, cancellationToken);
    }

    private Task SendAsync(string eventName, IEnumerable<string> groups, ClinicDashboardEventDto payload, CancellationToken cancellationToken)
    {
        var audience = groups
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct()
            .ToList();

        if (audience.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients.Groups(audience).SendAsync(eventName, payload, cancellationToken);
    }
}
