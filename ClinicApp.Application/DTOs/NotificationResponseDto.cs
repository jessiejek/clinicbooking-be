namespace ClinicApp.Application.DTOs;

public sealed class NotificationResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? NavigateTo { get; set; }
}
