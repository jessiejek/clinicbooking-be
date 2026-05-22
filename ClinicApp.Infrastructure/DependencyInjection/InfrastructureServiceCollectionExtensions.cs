using ClinicApp.Application.Common.Interfaces.Authentication;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Infrastructure.Authentication;
using ClinicApp.Infrastructure.Bookings;
using ClinicApp.Infrastructure.Doctors;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Patients;
using ClinicApp.Infrastructure.PatientDocuments;
using ClinicApp.Infrastructure.PatientMedia;
using ClinicApp.Infrastructure.Persistence;
using ClinicApp.Infrastructure.Seeding;
using ClinicApp.Infrastructure.Services;
using ClinicApp.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicApp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClinicSettingsService, ClinicSettingsService>();
        services.AddScoped<IIdentitySeeder, IdentitySeeder>();
        services.AddScoped<IClinicDoctorsService, DoctorsService>();
        services.AddScoped<IClinicBookingsService, BookingsService>();
        services.AddScoped<IClinicPaymentsService, BookingsService>();
        services.AddScoped<IPatientDocumentsService, PatientDocumentsService>();
        services.AddScoped<IPatientMediaService, PatientMediaService>();
        services.AddScoped<IPatientSeeder, PatientSeeder>();
        services.AddScoped<IBookingSeeder, BookingSeeder>();
        services.AddScoped<IClinicPatientsService, PatientsService>();
        services.AddScoped<IClinicServicesService, ClinicServicesService>();
        services.AddScoped<IClinicSeeder, ClinicSeeder>();

        return services;
    }
}
