using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest dto)
    {
        var result = await authService.LoginAsync(dto);
        return result.Error switch
        {
            AuthError.InvalidCredentials => BadRequest(),
            AuthError.UserInactive => StatusCode(StatusCodes.Status403Forbidden),
            null => Ok(result.Value),
            _ => StatusCode(500)
        };
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest dto)
    {
        var result = await authService.RegisterAsync(dto);
        return result.Error switch
        {
            AuthError.PasswordMismatch => BadRequest(),
            AuthError.WorkIdNotFound => NotFound(),
            AuthError.UserInactive => StatusCode(StatusCodes.Status403Forbidden),
            AuthError.UsernameTaken or AuthError.WorkIdAlreadyBound => Conflict(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => StatusCode(500)
        };
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest dto)
    {
        var result = await authService.RefreshAsync(dto.RefreshToken);
        return result.Error switch
        {
            AuthError.TokenInvalid => Unauthorized(),
            AuthError.UserInactive => StatusCode(StatusCodes.Status403Forbidden),
            null => Ok(result.Value),
            _ => StatusCode(500)
        };
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyRequest dto)
    {
        var valid = await authService.VerifyAsync(dto.Token);
        return valid ? Ok() : Unauthorized();
    }
}
