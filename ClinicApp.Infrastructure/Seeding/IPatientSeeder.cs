namespace ClinicApp.Infrastructure.Seeding;

public interface IPatientSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
