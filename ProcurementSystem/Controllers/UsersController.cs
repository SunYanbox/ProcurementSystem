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
}
