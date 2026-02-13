using ClinicPos.Api.Data;
using ClinicPos.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Controllers;

[ApiController]
[Route("api/v1/branches")]
public class BranchesController : ControllerBase
{
    private readonly ClinicPosDbContext _db;

    public BranchesController(ClinicPosDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var branches = await _db.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address
            })
            .ToListAsync();

        return Ok(branches);
    }
}
