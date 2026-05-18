using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;

namespace ClinicApp.API.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ClinicApp API",
                Version = "v1",
                Description = "ClinicApp backend API"
            });
        });

        services.AddFluentValidationAutoValidation();

        services.AddCors(options =>
        {
            options.AddPolicy("LocalDev", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200",
                        "http://127.0.0.1:4200",
                        "https://127.0.0.1:4200",
                        "http://localhost:8100",
                        "https://localhost:8100",
                        "http://127.0.0.1:8100",
                        "https://127.0.0.1:8100",
                        "capacitor://localhost",
                        "ionic://localhost")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
