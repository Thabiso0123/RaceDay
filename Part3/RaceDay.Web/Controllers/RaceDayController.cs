using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class RaceDayController : Controller
{
    protected readonly IRaceDayApi Api;

    protected RaceDayController(IRaceDayApi api) => Api = api;

    protected UserSession? CurrentUser => HttpContext.Session.GetUser();

    protected void Success(string message) => TempData["Success"] = message;
    protected void Failure(string message) => TempData["Error"] = message;

    protected IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        Failure("Your session has expired. Please sign in again.");
        return RedirectToAction("Login", "Account");
    }

    /// <summary>Turns a failed API call into the right page: login, 404 or a friendly message.</summary>
    protected IActionResult ApiError(ApiResult result)
    {
        if (result.IsUnauthorized) return SessionExpired();
        if (result.IsNotFound) return NotFound();
        return View("~/Views/Shared/ApiError.cshtml", new ErrorViewModel
        {
            Code = (int)result.Status,
            Title = "We could not complete that",
            Message = result.Error ?? "Something went wrong. Please try again."
        });
    }

    /// <summary>Shows API validation messages next to the form fields.</summary>
    protected void AddApiErrors(ApiResult result, string? prefix = null)
    {
        foreach (var (field, messages) in result.FieldErrors)
        {
            var key = field.StartsWith('$') ? "" : field;
            if (key != "" && prefix is not null) key = prefix + "." + key;
            foreach (var message in messages) ModelState.AddModelError(key, message);
        }
        if (result.Error is not null && result.FieldErrors.Count == 0)
        {
            ModelState.AddModelError("", result.Error);
        }
    }

    protected string ModelErrors() =>
        string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct());
}
