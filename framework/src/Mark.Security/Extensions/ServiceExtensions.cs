using System.Diagnostics;
using Mark.Security.Claims;
using Mark.Security.Users;

namespace Mark.Security.Extensions;

public static class CurrentUserExtensions
{
    public static string? FindClaimValue(this ICurrentUser currentUser, string claimType)
    {
        return currentUser.FindClaim(claimType)?.Value;
    }

    public static T FindClaimValue<T>(this ICurrentUser currentUser, string claimType)
        where T : struct
    {
        var value = currentUser.FindClaimValue(claimType);
        if (value == null)
        {
            return default;
        }

        return value.To<T>();
    }

    public static Guid GetId(this ICurrentUser currentUser)
    {
        Debug.Assert(currentUser.Id != null, "currentUser.Id != null");

        return currentUser!.Id!.Value;
    }

    public static Guid? FindImpersonatorTenantId(this ICurrentUser currentUser)
    {
        var impersonatorTenantId = currentUser.FindClaimValue(MarkClaimType.ImpersonatorTenantId);
        if (impersonatorTenantId.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(impersonatorTenantId, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static Guid? FindImpersonatorUserId(this ICurrentUser currentUser)
    {
        var impersonatorUserId = currentUser.FindClaimValue(MarkClaimType.ImpersonatorUserId);
        if (impersonatorUserId.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(impersonatorUserId, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static string? FindImpersonatorTenantName(this ICurrentUser currentUser)
    {
        return currentUser.FindClaimValue(MarkClaimType.ImpersonatorTenantName);
    }

    public static string? FindImpersonatorUserName(this ICurrentUser currentUser)
    {
        return currentUser.FindClaimValue(MarkClaimType.ImpersonatorUserName);
    }

    public static string GetSessionId(this ICurrentUser currentUser)
    {
        var sessionId = currentUser.FindSessionId();
        Debug.Assert(sessionId != null, "sessionId != null");
        return sessionId!;
    }

    public static string? FindSessionId(this ICurrentUser currentUser)
    {
        return currentUser.FindClaimValue(MarkClaimType.SessionId);
    }
}