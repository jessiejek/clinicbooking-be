using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Staff.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly IAdminStaffService _staffService;

    public StaffController(IAdminStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StaffMemberDto>>> GetStaff(CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetStaffAsync(cancellationToken);
        return Ok(staff);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<StaffMemberDto>> InviteStaff(
        [FromBody] CreateStaffInviteDto dto,
        CancellationToken cancellationToken)
    {
        var staff = await _staffService.InviteStaffAsync(dto, cancellationToken);
        return Ok(staff);
    }

    [HttpPut("invite/{inviteId:guid}/revoke")]
    public async Task<IActionResult> RevokeInvite(Guid inviteId, CancellationToken cancellationToken)
    {
        await _staffService.RevokeInviteAsync(inviteId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/update-status")]
    public async Task<ActionResult<StaffMemberDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStaffStatusDto dto,
        CancellationToken cancellationToken)
    {
        var staff = await _staffService.UpdateStaffStatusAsync(id, dto.Action, cancellationToken);
        return Ok(staff);
    }
}

public sealed record UpdateStaffStatusDto(string Action);
