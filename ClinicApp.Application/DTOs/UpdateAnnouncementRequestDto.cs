namespace ClinicApp.Application.DTOs;

public sealed class UpdateAnnouncementRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}
