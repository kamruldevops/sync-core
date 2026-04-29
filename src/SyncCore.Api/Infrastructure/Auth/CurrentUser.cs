using System.Security.Claims;

namespace SyncCore.Api.Infrastructure.Auth;

public static class CurrentUser
{
    // Falls back to a fixed dev user when B2C auth is not configured
    public static string GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? "local-dev-user";
}
