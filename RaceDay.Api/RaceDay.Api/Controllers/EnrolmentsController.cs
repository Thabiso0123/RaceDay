using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{id:int}/enrolments")]
[Authorize]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int id, EnrolRequest request)
    {
        var participantId = User.GetUserId();

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
        {
            return Problem("Event not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == id);
        if (category is null)
        {
            return Problem("Category not found for this event.", statusCode: StatusCodes.Status404NotFound);
        }

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == participantId && e.Category.EventId == id))
        {
            return Problem("You are already enrolled in this event.", statusCode: StatusCodes.Status409Conflict);
        }

        if (category.MaxParticipants.HasValue &&
            await _db.Enrolments.CountAsync(e => e.CategoryId == category.CategoryId) >= category.MaxParticipants.Value)
        {
            return Problem("This category is full.", statusCode: StatusCodes.Status409Conflict);
        }

        var participant = await _db.Participants.FindAsync(participantId);
        if (participant is null) return Unauthorized();

        category.Event = ev;
        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            Participant = participant,
            CategoryId = category.CategoryId,
            Category = category,
            EnrolmentDate = DateTime.Now,
            Status = "Confirmed"
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, enrolment.ToResponse());
    }

    [HttpGet]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetForEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.Category.EventId == id)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()));
    }
}
