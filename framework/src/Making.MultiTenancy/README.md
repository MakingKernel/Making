# Making.MultiTenancy

Multi-tenancy implementation and services for the Making framework.

## Overview

Making.MultiTenancy provides concrete implementations for multi-tenant applications in the Making framework. It includes current tenant management, async local tenant accessor, and dependency injection setup for building scalable multi-tenant applications.

## Features

- **Current Tenant Implementation**: Concrete implementation of current tenant access
- **Async Local Accessor**: Thread-safe tenant context using AsyncLocal
- **Tenant Context Switching**: Safe tenant context changes with disposal pattern
- **Dependency Injection**: Full DI container integration
- **Thread-Safe Operations**: Concurrent tenant access support

## Installation

```bash
dotnet add package Making.MultiTenancy
```

## Usage

### Register Services

```csharp
services.AddMarkMultiTenancy();
```

### Basic Multi-Tenancy Setup

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMarkMultiTenancy();
        
        // Register tenant-aware services
        services.AddScoped<ITenantAwareService, TenantAwareService>();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Add tenant resolution middleware
        app.UseMarkTenantResolution();
    }
}
```

### Tenant-Aware Service Implementation

```csharp
public class ProductService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IProductRepository _productRepository;
    
    public ProductService(ICurrentTenant currentTenant, IProductRepository productRepository)
    {
        _currentTenant = currentTenant;
        _productRepository = productRepository;
    }
    
    public async Task<List<Product>> GetProductsAsync()
    {
        if (!_currentTenant.IsAvailable)
        {
            throw new InvalidOperationException("No tenant context available");
        }
        
        var tenantId = _currentTenant.Id;
        return await _productRepository.GetByTenantAsync(tenantId);
    }
    
    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            TenantId = _currentTenant.Id,
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _productRepository.CreateAsync(product);
    }
    
    public async Task<Product> GetProductByIdAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        
        // Ensure product belongs to current tenant
        if (product?.TenantId != _currentTenant.Id)
        {
            throw new UnauthorizedAccessException("Product not accessible for current tenant");
        }
        
        return product;
    }
}
```

### Tenant Context Switching

```csharp
public class ReportingService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IOrderService _orderService;
    
    public ReportingService(ICurrentTenant currentTenant, IOrderService orderService)
    {
        _currentTenant = currentTenant;
        _orderService = orderService;
    }
    
    public async Task<TenantReport> GenerateTenantReportAsync(string tenantId)
    {
        // Switch to specific tenant context
        using (_currentTenant.Change(tenantId))
        {
            var orders = await _orderService.GetOrdersAsync();
            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var orderCount = orders.Count;
            
            return new TenantReport
            {
                TenantId = tenantId,
                TenantName = _currentTenant.Name,
                TotalOrders = orderCount,
                TotalRevenue = totalRevenue,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
    
    public async Task<List<TenantReport>> GenerateAllTenantsReportAsync(List<string> tenantIds)
    {
        var reports = new List<TenantReport>();
        
        foreach (var tenantId in tenantIds)
        {
            var report = await GenerateTenantReportAsync(tenantId);
            reports.Add(report);
        }
        
        return reports;
    }
}
```

### Multi-Tenant Database Context

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure global query filters for multi-tenancy
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => p.TenantId == _currentTenant.Id);
            
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == _currentTenant.Id);
            
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => c.TenantId == _currentTenant.Id);
        
        base.OnModelCreating(modelBuilder);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set tenant ID for new entities
        var tenantId = _currentTenant.Id;
        
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = tenantId;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Tenant Resolution Middleware

```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantResolver _tenantResolver;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ITenantResolver tenantResolver,
        ICurrentTenantAccessor tenantAccessor)
    {
        _next = next;
        _tenantResolver = tenantResolver;
        _tenantAccessor = tenantAccessor;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantInfo = await _tenantResolver.ResolveAsync(context);
        
        if (tenantInfo != null)
        {
            _tenantAccessor.Current = tenantInfo;
        }
        
        await _next(context);
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseMarkTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}
```

### Custom Tenant Resolver

```csharp
public interface ITenantResolver
{
    Task<ITenantInfo> ResolveAsync(HttpContext context);
}

public class HeaderTenantResolver : ITenantResolver
{
    private readonly ITenantStore _tenantStore;
    
    public HeaderTenantResolver(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }
    
    public async Task<ITenantInfo> ResolveAsync(HttpContext context)
    {
        // Resolve tenant from custom header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            return await _tenantStore.GetByIdAsync(tenantId);
        }
        
        // Resolve tenant from subdomain
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.').FirstOrDefault();
        
        if (!string.IsNullOrEmpty(subdomain))
        {
            return await _tenantStore.GetBySubdomainAsync(subdomain);
        }
        
        return null;
    }
}
```

## Requirements

- .NET Standard 2.0+
- Microsoft.Extensions.DependencyInjection.Abstractions
- Making.Core
- Making.MultiTenancy.Abstractions

## License

This project is part of the Making framework.