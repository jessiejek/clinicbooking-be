using ClinicApp.Application.Features.Staff.Dtos;

namespace ClinicApp.Application.Common.Interfaces;

public interface IAdminStaffService
{
    Task<List<StaffMemberDto>> GetStaffAsync(CancellationToken cancellationToken);
    Task<StaffMemberDto> InviteStaffAsync(CreateStaffInviteDto dto, CancellationToken cancellationToken);
    Task RevokeInviteAsync(Guid inviteId, CancellationToken cancellationToken);
    Task<StaffMemberDto> UpdateStaffStatusAsync(Guid id, string action, CancellationToken cancellationToken);
}
