using Making.Security.Claims;

namespace Making.AspNetCore.Security;

public class MakingClaimsMapOptions
{
    public Dictionary<string, Func<string>> Maps { get; }

    public MakingClaimsMapOptions()
    {
        Maps = new Dictionary<string, Func<string>>()
        {
            { "sub", () => MakingClaimType.UserId },
            { "role", () => MakingClaimType.Role },
            { "email", () => MakingClaimType.Email },
            { "name", () => MakingClaimType.UserName },
            { "family_name", () => MakingClaimType.SurName },
            { "given_name", () => MakingClaimType.Name }
        };
    }
}
