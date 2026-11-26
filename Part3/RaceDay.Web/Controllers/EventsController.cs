using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class EventsController : RaceDayController
{
    public EventsController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Details(int id)
    {
        var user = CurrentUser!;

        var ev = await Api.GetEventAsync(id);
        if (!ev.Success) return ApiError(ev);

        var categories = await Api.GetCategoriesAsync(id);
        if (!categories.Success) return ApiError(categories);

        var model = new EventDetailsViewModel
        {
            Event = ev.Data!,
            Categories = categories.Data!,
            IsOwner = user.IsOrganiser && ev.Data!.OrganiserId == user.Id
        };

        if (user.IsParticipant)
        {
            var mine = await Api.GetMyEnrolmentsAsync();
            if (mine.IsUnauthorized) return SessionExpired();
            if (mine.Success) model.MyEnrolment = mine.Data!.FirstOrDefault(e => e.EventId == id);
            model.CanEnrol = model.MyEnrolment is null && ev.Data!.Date >= DateOnly.FromDateTime(DateTime.Today);
        }

        return View(model);
    }
}
