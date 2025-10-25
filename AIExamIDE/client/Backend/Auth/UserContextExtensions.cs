using System.Security.Claims;

namespace AIExamIDE.Backend.Auth;

public static class UserContextExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal is null) return null;
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal?.FindFirstValue(ClaimTypes.Role);
    }
}
