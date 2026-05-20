namespace ClinicApp.Application.Features.Bookings.Dtos;

public sealed record SubmitProofDto(
    string ProofType,
    string ProofValue);
