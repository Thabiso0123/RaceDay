using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
