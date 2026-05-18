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

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer {token}'"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
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
