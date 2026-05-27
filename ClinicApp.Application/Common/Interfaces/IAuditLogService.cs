namespace ClinicApp.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string entityType, string entityId, string action, string performedBy, string? details = null, CancellationToken ct = default);
}
