using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{id:int}/results")]
[Authorize]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Capture(int id, ResultRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
        {
            return Problem("Event not found.", statusCode: StatusCodes.Status404NotFound);
        }
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.ParticipantId == request.ParticipantId && e.Category.EventId == id);
        if (enrolment is null)
        {
            return Problem("This participant is not enrolled in the event.", statusCode: StatusCodes.Status404NotFound);
        }

        if (enrolment.Result is not null)
        {
            return Problem("A result has already been captured for this participant.", statusCode: StatusCodes.Status409Conflict);
        }

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            Enrolment = enrolment,
            FinishTime = request.FinishTime,
            Position = request.Position,
            DateRecorded = DateTime.Now
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, result.ToResponse());
    }

    /// <summary>View your own result for an event (Participant only).</summary>
    /// <remarks>Returns 404 if the event does not exist or no result has been recorded for you yet.</remarks>
    [HttpGet]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> GetMine(int id)
    {
        var participantId = User.GetUserId();

        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.Enrolment.ParticipantId == participantId && r.Enrolment.Category.EventId == id);

        if (result is null) return NotFound();

        var dto = result.ToResponse();
        dto.TotalFinishers = await _db.Results.CountAsync(r => r.Enrolment.Category.EventId == id);
        return Ok(dto);
    }

    /// <summary>All captured results for an event, best position first (Organiser who owns the event).</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetAllForEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.Category.EventId == id)
            .ToListAsync();

        var ordered = results.OrderBy(r => r.Position ?? int.MaxValue).ToList();
        return Ok(ordered.Select(r =>
        {
            var dto = r.ToResponse();
            dto.TotalFinishers = ordered.Count;
            return dto;
        }));
    }
}
