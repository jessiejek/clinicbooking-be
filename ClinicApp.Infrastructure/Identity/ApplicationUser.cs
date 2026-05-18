using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? AuthProvider { get; set; }
    public string? ProviderUserId { get; set; }
    public bool IsFirstLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
