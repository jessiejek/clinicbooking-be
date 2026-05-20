namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record UpdateDoctorServicesDto(
    IReadOnlyCollection<Guid> ServiceIds);
