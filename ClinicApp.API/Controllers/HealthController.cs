using ClinicApp.API.Contracts.Health;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public HealthController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public ActionResult<HealthResponseDto> Get()
    {
        return Ok(new HealthResponseDto(
            Status: "healthy",
            AppName: _environment.ApplicationName,
            Environment: _environment.EnvironmentName,
            TimestampUtc: DateTimeOffset.UtcNow));
    }
}
