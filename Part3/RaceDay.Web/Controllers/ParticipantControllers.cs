using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles = "Participant")]
public class MyEnrolmentsController : RaceDayController
{
    public MyEnrolmentsController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        if (!result.Success) return ApiError(result);
        return View(result.Data!);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (eventId <= 0 || categoryId <= 0)
        {
            Failure("Please choose a category to enrol in.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await Api.EnrolAsync(eventId, categoryId);
        if (result.IsUnauthorized) return SessionExpired();
        if (!result.Success)
        {
            Failure(result.Error ?? "We could not complete your enrolment.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        Success($"You are enrolled in {result.Data!.EventName} ({result.Data.CategoryName}).");
        return RedirectToAction("Index");
    }
}

[SessionAuthorize(Roles = "Participant")]
public class MyResultsController : RaceDayController
{
    public MyResultsController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyResultsAsync();
        if (!result.Success) return ApiError(result);
        return View(result.Data!);
    }
}

[SessionAuthorize]
public class ProfileController : RaceDayController
{
    public ProfileController(IRaceDayApi api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync(CurrentUser!.Id);
        if (!result.Success) return ApiError(result);

        return View(new ProfilePageViewModel
        {
            Profile = result.Data!,
            Edit = new ProfileUpdateModel
            {
                Name = result.Data!.Name,
                Email = result.Data.Email,
                ContactNumber = result.Data.ContactNumber
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Update([Bind(Prefix = "Edit")] ProfileUpdateModel model)
    {
        var user = CurrentUser!;

        if (ModelState.IsValid)
        {
            var result = await Api.UpdateProfileAsync(user.Id, new UpdateProfileRequest
            {
                Name = model.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                ContactNumber = string.IsNullOrWhiteSpace(model.ContactNumber) ? null : model.ContactNumber.Trim()
            });

            if (result.IsUnauthorized) return SessionExpired();
            if (result.Success)
            {
                var p = result.Data!;
                HttpContext.Session.SetUser(new UserSession(p.Id, p.Name, p.Email, p.Role, p.ProfilePicture));
                Success("Your profile has been updated.");
                return RedirectToAction("Index");
            }
            AddApiErrors(result, "Edit");
        }

        // Show the page again with the problems highlighted.
        var profile = await Api.GetProfileAsync(user.Id);
        if (!profile.Success) return ApiError(profile);
        return View("Index", new ProfilePageViewModel { Profile = profile.Data!, Edit = model });
    }

    [HttpPost]
    [RequestSizeLimit(ImageRules.MaxBytes + 1_000_000)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        var user = CurrentUser!;

        if (picture is null)
        {
            Failure("Please choose an image to upload.");
            return RedirectToAction("Index");
        }

        var problem = ImageRules.Check(picture);
        if (problem is not null)
        {
            Failure(problem);
            return RedirectToAction("Index");
        }

        var result = await Api.UploadProfilePictureAsync(user.Id, picture);
        if (result.IsUnauthorized) return SessionExpired();
        if (!result.Success)
        {
            Failure(result.Error ?? "We could not upload that picture.");
            return RedirectToAction("Index");
        }

        var p = result.Data!;
        HttpContext.Session.SetUser(new UserSession(p.Id, p.Name, p.Email, p.Role, p.ProfilePicture));
        Success("Your profile picture has been updated.");
        return RedirectToAction("Index");
    }
}

public class MediaController : RaceDayController
{
    public MediaController(IRaceDayApi api) : base(api) { }

    /// <summary>Streams an image from the API so the browser never contacts Azure directly.</summary>
    public async Task<IActionResult> Get(string path)
    {
        var result = await Api.GetMediaAsync(path);
        if (!result.Success || result.Data is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(result.Data.Content, result.Data.ContentType);
    }
}
