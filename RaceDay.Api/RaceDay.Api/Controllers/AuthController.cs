using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private static readonly string[] AllowedRoles = { "Organiser", "Participant" };

    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<Participant> _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<Participant> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Register(RegisterRequest request)
    {
        var roleName = AllowedRoles.FirstOrDefault(r =>
            r.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase));
        if (roleName is null)
        {
            return Problem("Role must be either 'Organiser' or 'Participant'.", statusCode: StatusCodes.Status400BadRequest);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Participants.AnyAsync(p => p.Email == email))
        {
            return Problem("An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        var role = await _db.Roles.FirstAsync(r => r.RoleName == roleName);
        var user = new Participant
        {
            FullName = request.Name.Trim(),
            Email = email,
            ContactNumber = request.ContactNumber,
            RoleId = role.RoleId,
            Role = role,
            DateRegistered = DateTime.Now
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Participants.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Participants.Include(p => p.Role).FirstOrDefaultAsync(p => p.Email == email);

        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ParticipantId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(user.ToResponse());
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
