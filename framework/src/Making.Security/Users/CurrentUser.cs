using System.Security.Claims;
using Making.Security.Claims;
using Making.Security.Extensions;
using Making.Security.Principal;

namespace Making.Security.Users;

public class CurrentUser : ICurrentUser
{
    
    private static readonly Claim[] EmptyClaimsArray = new Claim[0];

    public virtual bool IsAuthenticated => Id.HasValue;

    public virtual Guid? Id => _principalAccessor.Principal?.FindUserId();

    public virtual string? UserName => this.FindClaimValue(MarkClaimType.UserName);

    public virtual string? Name => this.FindClaimValue(MarkClaimType.Name);

    public virtual string? SurName => this.FindClaimValue(MarkClaimType.SurName);

    public virtual string? PhoneNumber => this.FindClaimValue(MarkClaimType.PhoneNumber);

    public virtual bool PhoneNumberVerified => string.Equals(this.FindClaimValue(MarkClaimType.PhoneNumberVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual string? Email => this.FindClaimValue(MarkClaimType.Email);

    public virtual bool EmailVerified => string.Equals(this.FindClaimValue(MarkClaimType.EmailVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual Guid? TenantId => _principalAccessor.Principal?.FindTenantId();

    public virtual string[] Roles => FindClaims(MarkClaimType.Role).Select(c => c.Value).Distinct().ToArray();

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
        return FindClaims(MarkClaimType.Role).Any(c => c.Value == roleName);
    }
}