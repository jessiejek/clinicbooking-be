namespace ClinicApp.Infrastructure.Seeding;

public interface IBookingSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
