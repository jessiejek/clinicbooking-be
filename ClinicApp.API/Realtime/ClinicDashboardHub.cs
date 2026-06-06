using System.Security.Claims;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.API.Realtime;

[Authorize]
public sealed class ClinicDashboardHub : Hub
{
    private readonly AppDbContext _dbContext;

    public ClinicDashboardHub(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User is null)
        {
            await base.OnConnectedAsync();
            return;
        }

        if (Context.User.IsInRole("Admin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        if (Context.User.IsInRole("Staff"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
        }

        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var doctorId = await _dbContext.Doctors
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .SingleOrDefaultAsync();

            if (doctorId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Doctor:{doctorId.Value:D}");
            }

            var patientId = await _dbContext.Patients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .SingleOrDefaultAsync();

            if (patientId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Patient:{patientId.Value:D}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User is null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        if (Context.User.IsInRole("Admin"))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admin");
        }

        if (Context.User.IsInRole("Staff"))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Staff");
        }

        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var doctorId = await _dbContext.Doctors
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .SingleOrDefaultAsync();

            if (doctorId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Doctor:{doctorId.Value:D}");
            }

            var patientId = await _dbContext.Patients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .SingleOrDefaultAsync();

            if (patientId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Patient:{patientId.Value:D}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
