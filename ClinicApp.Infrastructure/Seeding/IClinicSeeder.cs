namespace ClinicApp.Infrastructure.Seeding;

public interface IClinicSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
