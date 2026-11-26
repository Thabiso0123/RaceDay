using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll([FromQuery] EventType? eventType)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();
        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<OrganiserEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<OrganiserEventResponse>>> GetMine()
    {
        var organiserId = User.GetUserId();

        var events = await _db.Events.Include(e => e.Organiser)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var enrolmentCounts = await _db.Enrolments
            .Where(en => en.Category.Event.OrganiserId == organiserId)
            .GroupBy(en => en.Category.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var categoryCounts = await _db.Categories
            .Where(c => c.Event.OrganiserId == organiserId)
            .GroupBy(c => c.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        return Ok(events.Select(e => new OrganiserEventResponse
        {
            Id = e.EventId,
            Name = e.EventName,
            Description = e.Description,
            Date = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser.FullName,
            BannerImage = e.BannerImage,
            EnrolmentCount = enrolmentCounts.GetValueOrDefault(e.EventId),
            CategoryCount = categoryCounts.GetValueOrDefault(e.EventId)
        }));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? NotFound() : Ok(ev.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var organiser = await _db.Participants.FindAsync(User.GetUserId());
        if (organiser is null) return Unauthorized();

        var ev = new Event
        {
            EventName = request.Name.Trim(),
            EventDate = request.Date,
            Location = request.Location,
            Description = request.Description,
            EventType = request.EventType,
            Distance = request.Distance,
            OrganiserId = organiser.ParticipantId,
            Organiser = organiser,
            CreatedDate = DateTime.Now
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ev.ToResponse());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        ev.EventName = request.Name.Trim();
        ev.EventDate = request.Date;
        ev.Location = request.Location;
        ev.Description = request.Description;
        ev.EventType = request.EventType;
        ev.Distance = request.Distance;
        await _db.SaveChangesAsync();

        return Ok(ev.ToResponse());
    }

 
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        if (await _db.Enrolments.AnyAsync(en => en.Category.EventId == id))
        {
            return Problem("This event has enrolments and cannot be deleted.", statusCode: StatusCodes.Status409Conflict);
        }

        var banner = ev.BannerImage;
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        await _blobs.DeleteAsync(banner);
        return NoContent();
    }

    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(ImageValidator.MaxBytes + 1_000_000)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EventResponse>> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        try
        {
            var newName = await _blobs.UploadAsync("events", file, ct);
            var oldName = ev.BannerImage;
            ev.BannerImage = newName;
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

        return Ok(ev.ToResponse());
    }
}
