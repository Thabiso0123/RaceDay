using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{id:int}/categories")]
[Authorize]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Create(int id, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventId = id,
            CategoryName = request.Name.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            Distance = request.Distance,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetForEvent(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id)) return NotFound();

        var categories = await _db.Categories.Where(c => c.EventId == id)
            .OrderBy(c => c.CategoryName).ToListAsync();
        return Ok(categories.Select(c => c.ToResponse()));
    }
}
