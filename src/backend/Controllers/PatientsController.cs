using ClinicPos.Api.Dtos;
using ClinicPos.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPos.Api.Controllers;

[ApiController]
[Route("api/v1/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreatePatients")]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var patient = await _patientService.CreateAsync(request);
        return CreatedAtAction(nameof(List), null, patient);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewPatients")]
    public async Task<IActionResult> List([FromQuery] Guid? branchId)
    {
        var patients = await _patientService.ListAsync(branchId);
        return Ok(patients);
    }
}
