using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Making.AspNetCore.Security;

public class MakingClaimsTransformation : IClaimsTransformation
{
    protected IOptions<MakingClaimsMapOptions> MakingClaimsMapOptions { get; }

    public MakingClaimsTransformation(IOptions<MakingClaimsMapOptions> makingClaimsMapOptions)
    {
        MakingClaimsMapOptions = makingClaimsMapOptions;
    }

    public virtual Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var mapClaims = principal.Claims.Where(claim => MakingClaimsMapOptions.Value.Maps.Keys.Contains(claim.Type));

        principal.AddIdentity(new ClaimsIdentity(mapClaims.Select(
                    claim => new Claim(
                        MakingClaimsMapOptions.Value.Maps[claim.Type](),
                        claim.Value,
                        claim.ValueType,
                        claim.Issuer
                    )
                )
            )
        );

        return Task.FromResult(principal);
    }
}