using ClinicApp.API.DependencyInjection;
using ClinicApp.API.Middleware;
using ClinicApp.Application.DependencyInjection;
using ClinicApp.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalDev");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
