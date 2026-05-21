namespace ClinicApp.Application.Common.Interfaces;

public interface IClinicRealtimeNotifier
{
    Task NotifyBookingEventAsync(
        string eventName,
        Guid bookingId,
        Guid patientId,
        Guid doctorId,
        string status,
        string paymentStatus,
        decimal? finalAmount,
        bool isProfessionalFeeWaived,
        CancellationToken cancellationToken);

    Task NotifyDoctorScheduleUpdatedAsync(Guid doctorId, CancellationToken cancellationToken);

    Task NotifyDoctorServicesUpdatedAsync(IEnumerable<Guid> doctorIds, CancellationToken cancellationToken);

    Task NotifyPatientProfileUpdatedAsync(Guid patientId, CancellationToken cancellationToken);
}
