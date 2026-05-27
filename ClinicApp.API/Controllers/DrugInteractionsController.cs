using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctor")]
[Route("api/drug-interactions")]
public sealed class DrugInteractionsController : ControllerBase
{
    /// <summary>
    /// Checks if a drug conflicts with known patient allergies.
    /// Returns unavailable=true if the drug interaction database is not configured.
    /// </summary>
    [HttpPost("allergy-check")]
    public ActionResult<object> AllergyCheck([FromBody] AllergyCheckRequest request)
    {
        return Ok(new
        {
            conflict = false,
            unavailable = true,
            message = "Drug-allergy check unavailable - verify manually before prescribing"
        });
    }

    /// <summary>
    /// Checks for interactions between a set of prescribed drugs.
    /// Returns unavailable=true if the drug interaction database is not configured.
    /// </summary>
    [HttpPost("check")]
    public ActionResult<object> Check([FromBody] InteractionCheckRequest request)
    {
        return Ok(new
        {
            unavailable = true,
            warnings = Array.Empty<object>()
        });
    }
}

public sealed record AllergyCheckRequest(
    string DrugName,
    string[] Allergies);

public sealed record InteractionCheckRequest(
    DrugItem[] Drugs);

public sealed record DrugItem(
    string MedicineName,
    string? GenericName,
    string? Strength,
    string? Route,
    string? Frequency);
