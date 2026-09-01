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
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var user = await userService.GetMeAsync(userId.Value);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await userService.ChangePasswordAsync(userId.Value, dto);
        return result.Error switch
        {
            UserError.WrongPassword or UserError.PasswordTooWeak => BadRequest(),
            UserError.UserNotFound => NotFound(),
            null => NoContent(),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPut("me/phone")]
    [Authorize]
    public async Task<ActionResult<UserDto>> BindPhone(BindPhoneRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await userService.BindPhoneAsync(userId.Value, dto);
        return result.Error switch
        {
            UserError.PhoneInvalid => BadRequest(),
            UserError.PhoneTaken => Conflict(),
            UserError.UserNotFound => NotFound(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpDelete("me/phone")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UnbindPhone()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await userService.UnbindPhoneAsync(userId.Value);
        return result.Error switch
        {
            UserError.UserNotFound => NotFound(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    private long? GetCurrentUserId()
    {
        // JwtBearer maps the JWT "sub" claim to ClaimTypes.NameIdentifier by default,
        // so read it through the framework-standard claim type.
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && long.TryParse(sub, out var userId) ? userId : null;
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
