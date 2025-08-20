using System.Security.Claims;

namespace Making.Security.Claims;

[Singleton]
public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessor
{
    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        return (Thread.CurrentPrincipal as ClaimsPrincipal)!;
    }
}
