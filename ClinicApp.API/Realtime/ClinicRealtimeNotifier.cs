using ClinicApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClinicApp.API.Realtime;

public sealed class ClinicRealtimeNotifier : IClinicRealtimeNotifier
{
    private readonly IHubContext<ClinicDashboardHub> _hubContext;

    public ClinicRealtimeNotifier(IHubContext<ClinicDashboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBookingEventAsync(
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
        var payload = new
        {
            eventName,
            bookingId = bookingId.ToString(),
            patientId = patientId.ToString(),
            doctorId = doctorId.ToString(),
            status,
            paymentStatus,
            finalAmount,
            isProfessionalFeeWaived,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        await _hubContext.Clients.Group("Admin").SendAsync(eventName, payload, cancellationToken);
        await _hubContext.Clients.Group("Staff").SendAsync(eventName, payload, cancellationToken);
        await _hubContext.Clients.Group($"Doctor:{doctorId:D}").SendAsync(eventName, payload, cancellationToken);
        await _hubContext.Clients.Group($"Patient:{patientId:D}").SendAsync(eventName, payload, cancellationToken);
    }

    public async Task NotifyDoctorScheduleUpdatedAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var payload = new { eventName = "DoctorScheduleUpdated", doctorId = doctorId.ToString(), timestamp = DateTime.UtcNow.ToString("o") };
        await _hubContext.Clients.Group($"Doctor:{doctorId:D}").SendAsync("DoctorScheduleUpdated", payload, cancellationToken);
        await _hubContext.Clients.Group("Staff").SendAsync("DoctorScheduleUpdated", payload, cancellationToken);
        await _hubContext.Clients.Group("Admin").SendAsync("DoctorScheduleUpdated", payload, cancellationToken);
    }

    public async Task NotifyDoctorServicesUpdatedAsync(IEnumerable<Guid> doctorIds, CancellationToken cancellationToken)
    {
        var payload = new { eventName = "DoctorServicesUpdated", timestamp = DateTime.UtcNow.ToString("o") };
        foreach (var doctorId in doctorIds)
        {
            await _hubContext.Clients.Group($"Doctor:{doctorId:D}").SendAsync("DoctorServicesUpdated", payload, cancellationToken);
        }
    }

    public async Task NotifyPatientProfileUpdatedAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var payload = new { eventName = "PatientProfileUpdated", patientId = patientId.ToString(), timestamp = DateTime.UtcNow.ToString("o") };
        await _hubContext.Clients.Group($"Patient:{patientId:D}").SendAsync("PatientProfileUpdated", payload, cancellationToken);
    }
}
