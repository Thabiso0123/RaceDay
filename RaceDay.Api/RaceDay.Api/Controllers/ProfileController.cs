using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public ProfileController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> Get(int id)
    {
        var user = await _db.Participants.Include(p => p.Role)
            .FirstOrDefaultAsync(p => p.ParticipantId == id);

        if (user is null) return NotFound();
        if (id != User.GetUserId()) return Forbid();

        return Ok(user.ToResponse());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Update(int id, UpdateProfileRequest request)
    {
        var user = await _db.Participants.Include(p => p.Role)
            .FirstOrDefaultAsync(p => p.ParticipantId == id);

        if (user is null) return NotFound();
        if (id != User.GetUserId()) return Forbid();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (email != user.Email)
            {
                if (await _db.Participants.AnyAsync(p => p.Email == email))
                {
                    return Problem("Another account already uses this email.",
                        statusCode: StatusCodes.Status409Conflict);
                }
                user.Email = email;
            }
        }

        user.FullName = request.Name.Trim();
        user.ContactNumber = request.ContactNumber;
        await _db.SaveChangesAsync();

        return Ok(user.ToResponse());
    }

    /// <summary>Upload or replace your own profile picture.</summary>
    /// <remarks>The image is validated (JPG, PNG, GIF or WEBP, max 5 MB) and stored in Azure Blob Storage.</remarks>
    [HttpPost("{id:int}/picture")]
    [RequestSizeLimit(ImageValidator.MaxBytes + 1_000_000)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProfileResponse>> UploadPicture(int id, IFormFile file, CancellationToken ct)
    {
        var user = await _db.Participants.Include(p => p.Role)
            .FirstOrDefaultAsync(p => p.ParticipantId == id, ct);

        if (user is null) return NotFound();
        if (id != User.GetUserId()) return Forbid();

        try
        {
            var newName = await _blobs.UploadAsync("profiles", file, ct);
            var oldName = user.ProfilePicture;
            user.ProfilePicture = newName;
            await _db.SaveChangesAsync(ct);
            await _blobs.DeleteAsync(oldName, ct);
        }
        catch (InvalidImageException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (BlobStorageUnavailableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(user.ToResponse());
    }
}
