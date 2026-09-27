using System.Security.Claims;

namespace Bipins.AI.Api.Authentication;

public static class TenantAccess
{
    public static bool CanAccess(ClaimsPrincipal user, string tenantId) =>
        user.Identity?.IsAuthenticated == true &&
        !string.IsNullOrWhiteSpace(user.FindFirst("tenantId")?.Value) &&
        !string.IsNullOrWhiteSpace(tenantId) &&
        (string.Equals(user.FindFirst("tenantId")!.Value, tenantId, StringComparison.Ordinal) ||
         user.IsInRole("Admin"));
}
