using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles = "Organiser")]
public abstract class OrganiserControllerBase : RaceDayController
{
    protected OrganiserControllerBase(IRaceDayApi api) : base(api) { }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Loads an event and confirms it belongs to the signed-in Organiser.</summary>
    protected async Task<(EventDto? Event, IActionResult? Problem)> GetOwnedEventAsync(int id)
    {
        var ev = await Api.GetEventAsync(id);
        if (!ev.Success) return (null, ApiError(ev));
        if (ev.Data!.OrganiserId != CurrentUser!.Id) return (null, RedirectToAction("AccessDenied", "Account"));
        return (ev.Data, null);
    }

    protected async Task<IActionResult> PickerAsync(string title, string intro, string controller, string buttonText)
    {
        var events = await Api.GetMyEventsAsync();
        if (!events.Success) return ApiError(events);

        return View("~/Views/Shared/EventPicker.cshtml", new EventPickerViewModel
        {
            Title = title,
            Intro = intro,
            Controller = controller,
            ButtonText = buttonText,
            Events = events.Data!
        });
    }
}

public class OrganiserController : OrganiserControllerBase
{
    public OrganiserController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Dashboard()
    {
        var events = await Api.GetMyEventsAsync();
        if (!events.Success) return ApiError(events);

        var upcoming = events.Data!.Where(e => e.Date >= Today).OrderBy(e => e.Date).ToList();
        return View(new DashboardViewModel
        {
            Events = events.Data!.OrderBy(e => e.Date).ToList(),
            UpcomingCount = upcoming.Count,
            NextEvent = upcoming.FirstOrDefault()
        });
    }
}

public class ManageEventsController : OrganiserControllerBase
{
    public ManageEventsController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var events = await Api.GetMyEventsAsync();
        if (!events.Success) return ApiError(events);
        return View(events.Data!);
    }

    [HttpGet]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { Date = Today.AddDays(30) });

    [HttpPost]
    [RequestSizeLimit(ImageRules.MaxBytes + 1_000_000)]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var created = await Api.CreateEventAsync(ToRequest(model));
        if (created.IsUnauthorized) return SessionExpired();
        if (!created.Success)
        {
            AddApiErrors(created);
            return View("Form", model);
        }

        var ev = created.Data!;
        if (model.Banner is not null)
        {
            var banner = await Api.UploadBannerAsync(ev.Id, model.Banner);
            if (banner.IsUnauthorized) return SessionExpired();
            if (!banner.Success)
            {
                Failure($"'{ev.Name}' was created, but the banner could not be uploaded: {banner.Error} You can add it by editing the event.");
                return RedirectToAction("Index", "Categories", new { eventId = ev.Id });
            }
        }

        Success($"'{ev.Name}' was created. Now add the categories people can enrol in.");
        return RedirectToAction("Index", "Categories", new { eventId = ev.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, problem) = await GetOwnedEventAsync(id);
        if (problem is not null) return problem;

        return View("Form", new EventFormViewModel
        {
            Id = ev!.Id,
            Name = ev.Name,
            Description = ev.Description,
            Date = ev.Date,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            CurrentBanner = ev.BannerImage
        });
    }

    [HttpPost]
    [RequestSizeLimit(ImageRules.MaxBytes + 1_000_000)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            await KeepCurrentBannerAsync(model);
            return View("Form", model);
        }

        var updated = await Api.UpdateEventAsync(id, ToRequest(model));
        if (updated.IsUnauthorized) return SessionExpired();
        if (!updated.Success)
        {
            if (updated.Status == System.Net.HttpStatusCode.NotFound || updated.Status == System.Net.HttpStatusCode.Forbidden)
                return ApiError(updated);
            AddApiErrors(updated);
            await KeepCurrentBannerAsync(model);
            return View("Form", model);
        }

        if (model.Banner is not null)
        {
            var banner = await Api.UploadBannerAsync(id, model.Banner);
            if (banner.IsUnauthorized) return SessionExpired();
            if (!banner.Success)
            {
                Failure($"The event was saved, but the banner could not be uploaded: {banner.Error}");
                return RedirectToAction("Index");
            }
        }

        Success("Event updated.");
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.IsUnauthorized) return SessionExpired();

        if (result.Success) Success("Event deleted.");
        else Failure(result.Error ?? "The event could not be deleted.");
        return RedirectToAction("Index");
    }

    private async Task KeepCurrentBannerAsync(EventFormViewModel model)
    {
        var ev = await Api.GetEventAsync(model.Id!.Value);
        if (ev.Success) model.CurrentBanner = ev.Data!.BannerImage;
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        Name = m.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        Date = m.Date!.Value,
        Location = string.IsNullOrWhiteSpace(m.Location) ? null : m.Location.Trim(),
        Distance = m.Distance,
        EventType = m.EventType
    };
}

public class CategoriesController : OrganiserControllerBase
{
    public CategoriesController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null)
            return await PickerAsync("Categories", "Choose an event to manage its age and distance categories.", "Categories", "Manage categories");

        return await BuildPageAsync(eventId.Value, new CategoryFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Add(int eventId, [Bind(Prefix = "Form")] CategoryFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var result = await Api.CreateCategoryAsync(eventId, new CategoryRequest
            {
                Name = form.Name.Trim(),
                MinAge = form.MinAge,
                MaxAge = form.MaxAge,
                Distance = form.Distance,
                MaxParticipants = form.MaxParticipants,
                EntryFee = form.EntryFee
            });

            if (result.IsUnauthorized) return SessionExpired();
            if (result.Success)
            {
                Success($"Category '{result.Data!.Name}' added.");
                return RedirectToAction("Index", new { eventId });
            }
            AddApiErrors(result, "Form");
        }

        return await BuildPageAsync(eventId, form);
    }

    private async Task<IActionResult> BuildPageAsync(int eventId, CategoryFormViewModel form)
    {
        var (ev, problem) = await GetOwnedEventAsync(eventId);
        if (problem is not null) return problem;

        var categories = await Api.GetCategoriesAsync(eventId);
        if (!categories.Success) return ApiError(categories);

        return View("Index", new CategoriesPageViewModel { Event = ev!, Categories = categories.Data!, Form = form });
    }
}

public class EventEnrolmentsController : OrganiserControllerBase
{
    public EventEnrolmentsController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null)
            return await PickerAsync("Enrolments", "Choose an event to see who has enrolled.", "EventEnrolments", "View enrolments");

        var (ev, problem) = await GetOwnedEventAsync(eventId.Value);
        if (problem is not null) return problem;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
        if (!enrolments.Success) return ApiError(enrolments);

        return View(new EventEnrolmentsViewModel { Event = ev!, Enrolments = enrolments.Data! });
    }
}

public class ResultsEntryController : OrganiserControllerBase
{
    public ResultsEntryController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null)
            return await PickerAsync("Results", "Choose an event to capture and publish finish times and positions.", "ResultsEntry", "Enter results");

        var (ev, problem) = await GetOwnedEventAsync(eventId.Value);
        if (problem is not null) return problem;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
        if (!enrolments.Success) return ApiError(enrolments);

        var results = await Api.GetEventResultsAsync(eventId.Value);
        if (!results.Success) return ApiError(results);

        var byParticipant = results.Data!.ToDictionary(r => r.ParticipantId);
        var rows = enrolments.Data!
            .Select(e => new ResultRow { Enrolment = e, Result = byParticipant.GetValueOrDefault(e.ParticipantId) })
            .OrderBy(r => r.Result is null ? 1 : 0)
            .ThenBy(r => r.Result?.Position ?? int.MaxValue)
            .ThenBy(r => r.Enrolment.ParticipantName)
            .ToList();

        return View(new ResultsEntryViewModel
        {
            Event = ev!,
            Rows = rows,
            EventHasFinished = ev!.Date <= Today
        });
    }

    [HttpPost]
    public async Task<IActionResult> Capture(ResultFormModel form)
    {
        if (!ModelState.IsValid)
        {
            Failure(ModelErrors());
            return RedirectToAction("Index", new { eventId = form.EventId });
        }

        var finish = new TimeSpan(form.Hours, form.Minutes, form.Seconds);
        if (finish <= TimeSpan.Zero)
        {
            Failure("Please enter a finish time greater than zero.");
            return RedirectToAction("Index", new { eventId = form.EventId });
        }

        var result = await Api.CaptureResultAsync(form.EventId, new ResultRequest
        {
            ParticipantId = form.ParticipantId,
            FinishTime = finish,
            Position = form.Position
        });

        if (result.IsUnauthorized) return SessionExpired();
        if (result.Success) Success($"Result published for {result.Data!.ParticipantName}.");
        else Failure(result.Error ?? "The result could not be saved.");

        return RedirectToAction("Index", new { eventId = form.EventId });
    }
}
