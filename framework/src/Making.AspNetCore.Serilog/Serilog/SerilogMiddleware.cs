using Making.MultiTenancy.Abstractions.MultiTenancy;
using Making.Security.Users;

namespace Making.AspNetCore.Serilog.Serilog;

[Singleton]
public class SerilogMiddleware : IMiddleware
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public SerilogMiddleware(ICurrentUser currentUser, ICurrentTenant currentTenant)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        
        await next(context);
    }
}