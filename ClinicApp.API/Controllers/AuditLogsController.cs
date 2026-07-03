using ClinicApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin,Staff,Doctor")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponseDto>>> GetLogs(CancellationToken ct)
    {
        var logs = await _auditLogService.GetLogsAsync(ct);
        return Ok(logs);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuditLogRequest request, CancellationToken ct)
    {
        var performedBy = User.FindFirstValue("fullName")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";
        await _auditLogService.LogAsync(request.EntityType, request.EntityId, request.Action, performedBy, request.Details, ct);
        return NoContent();
    }
}

public sealed class CreateAuditLogRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
}
