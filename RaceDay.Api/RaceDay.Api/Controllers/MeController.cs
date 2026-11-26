using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;

namespace RaceDay.Api.Controllers;

/// <summary>Everything the logged-in Participant needs for their own pages.</summary>
[ApiController]
[Route("api/me")]
[Authorize(Roles = "Participant")]
[Produces("application/json")]
public class MeController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public MeController(RaceDayDbContext db) => _db = db;

    /// <summary>All events the Participant has enrolled in, soonest event first.</summary>
    [HttpGet("enrolments")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> MyEnrolments()
    {
        var participantId = User.GetUserId();

        var enrolments = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.ParticipantId == participantId)
            .ToListAsync();

        return Ok(enrolments
            .OrderByDescending(e => e.Category.Event.EventDate)
            .Select(e => e.ToResponse()));
    }

    /// <summary>The Participant's race history across all events with a captured result.</summary>
    [HttpGet("results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> MyResults()
    {
        var participantId = User.GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .ToListAsync();

        var eventIds = results.Select(r => r.Enrolment.Category.EventId).Distinct().ToList();
        var totals = await _db.Results
            .Where(r => eventIds.Contains(r.Enrolment.Category.EventId))
            .GroupBy(r => r.Enrolment.Category.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        return Ok(results
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(r =>
            {
                var dto = r.ToResponse();
                dto.TotalFinishers = totals.GetValueOrDefault(dto.EventId);
                return dto;
            }));
    }
}
