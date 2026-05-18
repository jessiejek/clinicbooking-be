using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Infrastructure.Authentication;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var roles = new[] { "Admin", "Staff", "Doctor", "Patient" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await SeedUserAsync(
            "admin@gavino.clinic",
            "Admin@123456",
            "Admin",
            "Dr. Grace E. Gavino",
            false,
            cancellationToken);
        await SeedUserAsync("admin2@gavino.clinic", "Admin@123456", "Admin", "Maria Fernandez", false, cancellationToken);
        await SeedUserAsync("staff@gavino.clinic", "Staff@123456", "Staff", "Ana Gomez", false, cancellationToken);
        await SeedUserAsync("dr.santos@gavino.clinic", "Doctor@123456", "Doctor", "Dr. Santos", false, cancellationToken);
        await SeedUserAsync("dr.reyes@gavino.clinic", "Doctor@123456", "Doctor", "Dr. Jose Reyes", true, cancellationToken);
        await SeedUserAsync("dr.cruz@gavino.clinic", "Doctor@123456", "Doctor", "Dr. Ana Cruz", false, cancellationToken);
        await SeedUserAsync("patient@gavino.clinic", "Patient@123456", "Patient", "Juan dela Cruz", false, cancellationToken);
    }

    private async Task SeedUserAsync(
        string email,
        string password,
        string role,
        string fullName,
        bool isFirstLogin,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            Role = role,
            AuthProvider = "Local",
            IsActive = true,
            IsFirstLogin = isFirstLogin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(user, role);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
