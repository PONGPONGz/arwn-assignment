using ClinicPos.Api.Dtos;
using ClinicPos.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPos.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(List), null, user);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var users = await _userService.ListAsync();
        return Ok(users);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var user = await _userService.AssignRoleAsync(id, request);
        return Ok(user);
    }

    [HttpPut("{id}/branches")]
    public async Task<IActionResult> AssociateBranches(Guid id, [FromBody] AssociateBranchesRequest request)
    {
        var user = await _userService.AssociateBranchesAsync(id, request);
        return Ok(user);
    }
}
