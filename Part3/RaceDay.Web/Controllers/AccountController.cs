using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : RaceDayController
{
    public AccountController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser is not null) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so nothing from a previous visitor is reused.
        HttpContext.Session.Clear();

        var result = await Api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "We could not sign you in.");
            model.Password = "";
            return View(model);
        }

        var profile = result.Data;
        HttpContext.Session.SetUser(new UserSession(profile.Id, profile.Name, profile.Email, profile.Role, profile.ProfilePicture));

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUser is not null) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.RegisterAsync(new RegisterRequest
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            ContactNumber = string.IsNullOrWhiteSpace(model.ContactNumber) ? null : model.ContactNumber.Trim(),
            Role = model.Role
        });

        if (!result.Success)
        {
            AddApiErrors(result);
            model.Password = "";
            model.ConfirmPassword = "";
            return View(model);
        }

        Success("Your account is ready. Sign in to get started.");
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (CurrentUser is not null)
        {
            await Api.LogoutAsync();   // best effort: we clear our own session either way
        }
        HttpContext.Session.Clear();
        Success("You have been signed out.");
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
