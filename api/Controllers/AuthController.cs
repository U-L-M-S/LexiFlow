using LexiFlow.Api.Data;
using LexiFlow.Api.Dtos;
using LexiFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexiFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(ApplicationDbContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var normalized = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var passwordValid = PasswordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return Unauthorized();
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new LoginResponseDto(token));
    }
}
