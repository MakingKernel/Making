using System.Security.Claims;
using Making.Security.Claims;
using Making.Security.Extensions;
using Making.Security.Principal;

namespace Making.Security.Users;

[Transient]
public class CurrentUser : ICurrentUser
{
    
    private static readonly Claim[] EmptyClaimsArray = new Claim[0];

    public virtual bool IsAuthenticated => Id.HasValue;

    public virtual Guid? Id => _principalAccessor.Principal?.FindUserId();

    public virtual string? UserName => this.FindClaimValue(MakingClaimType.UserName);

    public virtual string? Name => this.FindClaimValue(MakingClaimType.Name);

    public virtual string? SurName => this.FindClaimValue(MakingClaimType.SurName);

    public virtual string? PhoneNumber => this.FindClaimValue(MakingClaimType.PhoneNumber);

    public virtual bool PhoneNumberVerified => string.Equals(this.FindClaimValue(MakingClaimType.PhoneNumberVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual string? Email => this.FindClaimValue(MakingClaimType.Email);

    public virtual bool EmailVerified => string.Equals(this.FindClaimValue(MakingClaimType.EmailVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual Guid? TenantId => _principalAccessor.Principal?.FindTenantId();

    public virtual string[] Roles => FindClaims(MakingClaimType.Role).Select(c => c.Value).Distinct().ToArray();

    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public CurrentUser(ICurrentPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }

    public virtual Claim? FindClaim(string claimType)
    {
        return _principalAccessor.Principal?.Claims.FirstOrDefault(c => c.Type == claimType);
    }

    public virtual Claim[] FindClaims(string claimType)
    {
        return _principalAccessor.Principal?.Claims.Where(c => c.Type == claimType).ToArray() ?? EmptyClaimsArray;
    }

    public virtual Claim[] GetAllClaims()
    {
        return _principalAccessor.Principal?.Claims.ToArray() ?? EmptyClaimsArray;
    }

    public virtual bool IsInRole(string roleName)
    {
        return FindClaims(MakingClaimType.Role).Any(c => c.Value == roleName);
    }
}