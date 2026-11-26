using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>The signed-in user, kept in server-side session.</summary>
public record UserSession(int Id, string Name, string Email, string Role, string? ProfilePicture)
{
    [JsonIgnore] public bool IsOrganiser => Role == "Organiser";
    [JsonIgnore] public bool IsParticipant => Role == "Participant";
}

public static class SessionExtensions
{
    private const string UserKey = "raceday.user";
    private const string ApiCookieKey = "raceday.apicookie";

    public static UserSession? GetUser(this ISession session)
    {
        var json = session.GetString(UserKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<UserSession>(json); }
        catch (JsonException) { return null; }
    }

    public static void SetUser(this ISession session, UserSession user) =>
        session.SetString(UserKey, JsonSerializer.Serialize(user));

    public static string? GetApiCookie(this ISession session) => session.GetString(ApiCookieKey);
    public static void SetApiCookie(this ISession session, string value) => session.SetString(ApiCookieKey, value);
    public static void ClearApiCookie(this ISession session) => session.Remove(ApiCookieKey);
}

/// <summary>Blocks unauthenticated users and users in the wrong role.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Comma separated list of allowed roles. Empty means any signed-in user.</summary>
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetUser();
        if (user is null)
        {
            var request = context.HttpContext.Request;
            context.Result = new RedirectToActionResult("Login", "Account",
                new { returnUrl = request.Path + request.QueryString });
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles) &&
            !Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(user.Role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

/// <summary>
/// Carries the API's authentication cookie for each user. The cookie is stored in that
/// user's session and attached to every call the MVC app makes to the API.
/// </summary>
public class ApiSessionHandler : DelegatingHandler
{
    private const string CookieName = "RaceDay.Auth";
    private readonly IHttpContextAccessor _accessor;

    public ApiSessionHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;
        var cookie = session?.GetApiCookie();
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{CookieName}={cookie}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var raw in values)
                {
                    if (!raw.StartsWith(CookieName + "=", StringComparison.Ordinal)) continue;
                    var value = raw[(CookieName.Length + 1)..].Split(';')[0];
                    if (string.IsNullOrEmpty(value)) session.ClearApiCookie();
                    else session.SetApiCookie(value);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(cookie))
            {
                // The API no longer accepts our cookie: the session is over.
                session.Clear();
            }
        }

        return response;
    }
}
