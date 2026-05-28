using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AnnouncementResponseDto>> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequestDto dto,
        CancellationToken ct)
    {
        var announcement = await _announcementService.CreateAnnouncementAsync(dto, ct);
        return CreatedAtAction(nameof(GetAnnouncements), new { id = announcement.Id }, announcement);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AnnouncementResponseDto>> UpdateAnnouncement(
        Guid id,
        [FromBody] UpdateAnnouncementRequestDto dto,
        CancellationToken ct)
    {
        var announcement = await _announcementService.UpdateAnnouncementAsync(id, dto, ct);
        return Ok(announcement);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken ct)
    {
        await _announcementService.DeleteAnnouncementAsync(id, ct);
        return NoContent();
    }
}
