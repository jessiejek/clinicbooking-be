using FluentValidation.AspNetCore;
using ClinicApp.API.Serialization;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using ClinicApp.API.Swagger;
namespace ClinicApp.API.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ClinicApp API",
                Version = "v1",
                Description = "ClinicApp backend API"
            });

            // Use fully qualified names so Swagger doesn't collapse distinct DTOs into the same schema ID.
            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

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

            // Register custom operation filter for file uploads
            options.OperationFilter<FileUploadOperationFilter>();
        });

        services.AddFluentValidationAutoValidation();

        return services;
    }
}
