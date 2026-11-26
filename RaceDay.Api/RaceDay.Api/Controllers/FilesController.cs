using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Serves images stored in Azure Blob Storage so clients never talk to Azure directly.</summary>
[ApiController]
[Route("api/files")]
public partial class FilesController : ControllerBase
{
    private readonly IBlobStorageService _blobs;

    public FilesController(IBlobStorageService blobs) => _blobs = blobs;

    [GeneratedRegex(@"^(events|profiles)/[a-f0-9]{32}\.(jpg|png|gif|webp)$")]
    private static partial Regex BlobNamePattern();

    [HttpGet("{**path}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(string path, CancellationToken ct)
    {
        if (!BlobNamePattern().IsMatch(path)) return NotFound();

        try
        {
            var blob = await _blobs.OpenReadAsync(path, ct);
            if (blob is null) return NotFound();

            // Blob names are unique per upload, so the content never changes.
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(blob.Content, blob.ContentType);
        }
        catch (BlobStorageUnavailableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
