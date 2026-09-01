using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        // JwtBearer maps the JWT "sub" claim to ClaimTypes.NameIdentifier by default,
        // so read it through the framework-standard claim type.
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !long.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await userService.GetMeAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest dto)
    {
        var result = await userService.CreateAsync(dto);
        return result.Error switch
        {
            UserError.WorkIdTaken or UserError.EmailTaken or UserError.PhoneTaken
                => Conflict(),
            UserError.RoleInvalid or UserError.DepartmentNotFound
                => BadRequest(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(
        [FromQuery] long? departmentId,
        [FromQuery] string? role,
        [FromQuery] bool? working,
        [FromQuery] string? search)
    {
        var users = await userService.ListAsync(departmentId, role, working, search);
        return Ok(users);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> GetById(long id)
    {
        var result = await userService.GetByIdAsync(id);
        return result.Error switch
        {
            UserError.UserNotFound => NotFound(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Update(long id, UpdateUserRequest dto)
    {
        var result = await userService.UpdateAsync(id, dto);
        return result.Error switch
        {
            UserError.UserNotFound => NotFound(),
            UserError.EmailTaken or UserError.PhoneTaken => Conflict(),
            UserError.RoleInvalid or UserError.DepartmentNotFound => BadRequest(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }
}
