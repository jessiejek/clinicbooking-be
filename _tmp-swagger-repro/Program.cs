using ClinicApp.API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

var services = new ServiceCollection();
services.AddLogging();
services
    .AddControllers()
    .AddApplicationPart(typeof(PatientsController).Assembly)
    .AddApplicationPart(typeof(PatientDocumentsController).Assembly);
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Swagger Repro",
        Version = "v1"
    });
});

var provider = services.BuildServiceProvider();
try
{
    var swaggerProvider = provider.GetRequiredService<ISwaggerProvider>();
    var doc = swaggerProvider.GetSwagger("v1");
    Console.WriteLine($"OK: paths={doc.Paths.Count}");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}
