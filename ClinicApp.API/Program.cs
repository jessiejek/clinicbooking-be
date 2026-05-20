using System.Text;
using ClinicApp.API.DependencyInjection;
using ClinicApp.API.Middleware;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Options;
using ClinicApp.Application.DependencyInjection;
using ClinicApp.Infrastructure.DependencyInjection;
using ClinicApp.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services
    .AddOptions<GoogleAuthOptions>()
    .Bind(builder.Configuration.GetSection(GoogleAuthOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Google:ClientId is required for Google login.")
    .ValidateOnStart();
builder.Services.Configure<FacebookAuthOptions>(builder.Configuration.GetSection(FacebookAuthOptions.SectionName));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:8100",
                "https://localhost:8100")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("LocalDev");
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClinicApp.Infrastructure.Persistence.AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.MigrateAsync();
    await ClinicApp.Infrastructure.Persistence.AuthSchemaBootstrapper.EnsureApplicationUserColumnsAsync(dbContext, CancellationToken.None);
    await ClinicApp.Infrastructure.Persistence.AuthSchemaBootstrapper.EnsureCustomAuthTablesAsync(dbContext, CancellationToken.None);

    var seeder = scope.ServiceProvider.GetRequiredService<ClinicApp.Infrastructure.Authentication.IIdentitySeeder>();
    await seeder.SeedAsync(CancellationToken.None);

    var patientSeeder = scope.ServiceProvider.GetRequiredService<IPatientSeeder>();
    await patientSeeder.SeedAsync(CancellationToken.None);

    var clinicSeeder = scope.ServiceProvider.GetRequiredService<IClinicSeeder>();
    await clinicSeeder.SeedAsync(CancellationToken.None);

    var bookingSeeder = scope.ServiceProvider.GetRequiredService<IBookingSeeder>();
    await bookingSeeder.SeedAsync(CancellationToken.None);

    var clinicSettingsService = scope.ServiceProvider.GetRequiredService<IClinicSettingsService>();
    await clinicSettingsService.GetAsync(CancellationToken.None);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
