namespace ClinicApp.Infrastructure.Authentication;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
