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
    /// 
    /// NOTE: This is a deliberate stub. The drug interaction database is NOT configured.
    /// The endpoint always returns unavailable=true to signal that automated
    /// drug-allergy checking is not yet operational. Prescribers must verify
    /// drug-allergy compatibility manually before writing prescriptions.
    /// 
    /// Response shape:
    ///   { "conflict": false, "unavailable": true, "message": "...verify manually..." }
    /// 
    /// The frontend (AllergyWarningBannerComponent) handles this by running
    /// client-side cross-referencing of recorded allergies against prescription
    /// items, so the unavailable backend is a fallback, not a gap.
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
    /// 
    /// NOTE: This is a deliberate stub — drug interaction database is NOT configured.
    /// Always returns unavailable=true to signal that automated DDInter check
    /// is not yet operational. Prescribers must evaluate interactions manually.
    /// 
    /// Response shape:
    ///   { "unavailable": true, "warnings": [] }
    /// 
    /// The frontend runs a local drug-class conflict check as a basic safety net.
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
