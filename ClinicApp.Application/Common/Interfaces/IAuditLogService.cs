namespace ClinicApp.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string entityType, string entityId, string action, string performedBy, string? details = null, CancellationToken ct = default);
    Task<List<AuditLogResponseDto>> GetLogsAsync(CancellationToken ct = default);
}

public sealed class AuditLogResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string PerformedAt { get; set; } = string.Empty;
    public string? Details { get; set; }
}
