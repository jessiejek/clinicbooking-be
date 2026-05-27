using System.Net;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Staff.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Staff;

public sealed class AdminStaffService : IAdminStaffService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    public AdminStaffService(UserManager<ApplicationUser> userManager, AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<List<StaffMemberDto>> GetStaffAsync(CancellationToken cancellationToken)
    {
        var result = new List<StaffMemberDto>();

        // Active staff users from Identity
        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
        foreach (var user in staffUsers)
        {
            result.Add(new StaffMemberDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                Role: "Staff",
                Status: user.IsActive ? "Active" : "Inactive",
                IsInvite: false));
        }

        // Pending invites
        var invites = await _dbContext.StaffInvites
            .Where(i => i.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var invite in invites)
        {
            result.Add(new StaffMemberDto(
                Id: invite.Id.ToString(),
                FullName: invite.FullName,
                Email: invite.Email,
                Role: "Staff",
                Status: "Invited",
                IsInvite: true));
        }

        return result;
    }

    public async Task<StaffMemberDto> InviteStaffAsync(CreateStaffInviteDto dto, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.StaffInvites
            .FirstOrDefaultAsync(i => i.Email == dto.Email && i.Status == "Pending", cancellationToken);

        if (existing is not null)
        {
            throw new ApiException(HttpStatusCode.Conflict, "An invite for this email is already pending.");
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            throw new ApiException(HttpStatusCode.Conflict, "A user with this email already exists.");
        }

        var invite = new StaffInvite
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.StaffInvites.Add(invite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StaffMemberDto(
            Id: invite.Id.ToString(),
            FullName: invite.FullName,
            Email: invite.Email,
            Role: "Staff",
            Status: "Invited",
            IsInvite: true);
    }

    public async Task RevokeInviteAsync(Guid inviteId, CancellationToken cancellationToken)
    {
        var invite = await _dbContext.StaffInvites.FindAsync(new object[] { inviteId }, cancellationToken);
        if (invite is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Staff invite not found.");
        }

        invite.Status = "Revoked";
        invite.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffMemberDto> UpdateStaffStatusAsync(Guid id, string action, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Staff member not found.");
        }

        user.IsActive = action switch
        {
            "ban" => false,
            "unban" => true,
            _ => throw new ApiException(HttpStatusCode.BadRequest, "Invalid action. Use 'ban' or 'unban'.")
        };

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new StaffMemberDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email ?? string.Empty,
            Role: "Staff",
            Status: user.IsActive ? "Active" : "Inactive",
            IsInvite: false);
    }
}
