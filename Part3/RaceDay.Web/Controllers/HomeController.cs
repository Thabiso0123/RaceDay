using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : RaceDayController
{
    public HomeController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index(EventType? type, string? q)
    {
        var user = CurrentUser;
        if (user is null) return View("Landing");
        if (user.IsOrganiser) return RedirectToAction("Dashboard", "Organiser");

        var events = await Api.GetEventsAsync(type);
        if (!events.Success) return ApiError(events);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = events.Data!.Where(e => e.Date >= today);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            upcoming = upcoming.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Location?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var model = new HomeViewModel
        {
            Events = upcoming.OrderBy(e => e.Date).ToList(),
            SelectedType = type,
            Query = q
        };

        var mine = await Api.GetMyEnrolmentsAsync();
        if (mine.IsUnauthorized) return SessionExpired();
        if (mine.Success) model.EnrolledEventIds = mine.Data!.Select(e => e.EventId).ToHashSet();

        return View(model);
    }

    // Error pages are re-executed for POSTs too, so they must not demand an anti-forgery token.
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        Code = 500,
        Title = "Something went wrong",
        Message = "An unexpected problem occurred. Please try again, and if it keeps happening let us know."
    });

    [IgnoreAntiforgeryToken]
    public IActionResult StatusPage(int id)
    {
        var model = id switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "Page not found", Message = "We could not find the page you were looking for." },
            403 => new ErrorViewModel { Code = 403, Title = "Not allowed", Message = "You do not have permission to view that page." },
            _ => new ErrorViewModel { Code = id, Title = "Something went wrong", Message = "The request could not be completed." }
        };
        Response.StatusCode = id;
        return View("Error", model);
    }
}
