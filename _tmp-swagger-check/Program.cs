using System.Reflection;
using System.Runtime.Loader;
using ClinicApp.API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

var outputDir = @"D:\PROJECT AI\clinic1a-be\ClinicApp.API\bin\Debug\net8.0";

AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
{
    var candidate = Path.Combine(outputDir, $"{assemblyName.Name}.dll");
    return File.Exists(candidate)
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate)
        : null;
};

var apiAssemblyPath = Path.Combine(outputDir, "ClinicApp.API.dll");
var apiAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(apiAssemblyPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddApplicationPart(apiAssembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ClinicApp.API.Serialization.DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new ClinicApp.API.Serialization.TimeOnlyJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ClinicApp API",
        Version = "v1",
        Description = "ClinicApp backend API"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

try
{
    var provider = builder.Services.BuildServiceProvider();
    var swaggerProvider = provider.GetRequiredService<ISwaggerProvider>();
    var doc = swaggerProvider.GetSwagger("v1");
    Console.WriteLine($"Swagger generated successfully with {doc.Paths.Count} paths and {doc.Components.Schemas.Count} schemas.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    return 1;
}

return 0;
