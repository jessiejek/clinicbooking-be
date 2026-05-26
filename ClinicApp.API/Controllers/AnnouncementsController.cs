using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/announcements")]
public sealed class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnnouncementResponseDto>>> GetAnnouncements(CancellationToken ct)
    {
        var announcements = await _announcementService.GetActiveAnnouncementsAsync(ct);
        return Ok(announcements);
    }
}
