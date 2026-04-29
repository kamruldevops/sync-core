using System.Security.Claims;

namespace SyncCore.Api.Infrastructure.Auth;

public static class CurrentUser
{
    public static string GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? throw new UnauthorizedAccessException("No user identity found.");
}
