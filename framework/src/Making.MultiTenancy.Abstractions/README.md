# Making.MultiTenancy.Abstractions

Multi-tenancy abstractions and interfaces for the Making framework.

## Overview

Making.MultiTenancy.Abstractions provides the core abstractions and interfaces for implementing multi-tenant applications with the Making framework. It defines contracts for tenant information, current tenant access, and tenant resolution strategies.

## Features

- **Tenant Abstractions**: Core interfaces for tenant management
- **Current Tenant Access**: Interface for accessing current tenant context
- **Tenant Information**: Basic tenant information model
- **Provider Abstraction**: Abstraction layer for different tenancy strategies
- **Thread-Safe Access**: Safe concurrent access to tenant context

## Installation

```bash
dotnet add package Making.MultiTenancy.Abstractions
```

## Usage

### Tenant Information Interface

```csharp
public interface ITenantInfo
{
    string Id { get; }
    string Name { get; }
    string ConnectionString { get; }
    Dictionary<string, object> Properties { get; }
}

public class BasicTenantInfo : ITenantInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ConnectionString { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
```

### Current Tenant Interface

```csharp
public interface ICurrentTenant
{
    string Id { get; }
    string Name { get; }
    bool IsAvailable { get; }
    ITenantInfo TenantInfo { get; }
    
    IDisposable Change(string tenantId);
}
```

### Current Tenant Accessor

```csharp
public interface ICurrentTenantAccessor
{
    ITenantInfo Current { get; set; }
}
```

### Using in Services

```csharp
public class OrderService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IOrderRepository _orderRepository;
    
    public OrderService(ICurrentTenant currentTenant, IOrderRepository orderRepository)
    {
        _currentTenant = currentTenant;
        _orderRepository = orderRepository;
    }
    
    public async Task<List<Order>> GetOrdersAsync()
    {
        // Automatically filter by current tenant
        var tenantId = _currentTenant.Id;
        return await _orderRepository.GetByTenantAsync(tenantId);
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            TenantId = _currentTenant.Id,
            CustomerName = request.CustomerName,
            Items = request.Items,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _orderRepository.CreateAsync(order);
    }
}
```

### Tenant Context Switching

```csharp
public class AdminService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IUserService _userService;
    
    public AdminService(ICurrentTenant currentTenant, IUserService userService)
    {
        _currentTenant = currentTenant;
        _userService = userService;
    }
    
    public async Task<List<User>> GetAllUsersAcrossTenantsAsync(List<string> tenantIds)
    {
        var allUsers = new List<User>();
        
        foreach (var tenantId in tenantIds)
        {
            // Switch tenant context
            using (_currentTenant.Change(tenantId))
            {
                var tenantUsers = await _userService.GetUsersAsync();
                allUsers.AddRange(tenantUsers);
            }
        }
        
        return allUsers;
    }
}
```

### Custom Tenant Implementation

```csharp
public class CompanyTenant : ITenantInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ConnectionString { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    
    // Custom properties
    public string CompanyCode { get; set; }
    public string TimeZone { get; set; }
    public string CurrencyCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CompanyTenantService
{
    private readonly ICurrentTenant _currentTenant;
    
    public CompanyTenantService(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }
    
    public CompanyTenant GetCurrentCompany()
    {
        return _currentTenant.TenantInfo as CompanyTenant;
    }
    
    public string GetCurrentTimeZone()
    {
        var company = GetCurrentCompany();
        return company?.TimeZone ?? "UTC";
    }
    
    public string GetCurrentCurrency()
    {
        var company = GetCurrentCompany();
        return company?.CurrencyCode ?? "USD";
    }
}
```

### Repository Pattern with Multi-Tenancy

```csharp
public interface ITenantAwareRepository<T> where T : class
{
    Task<T> GetByIdAsync(object id);
    Task<List<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(object id);
}

public abstract class TenantAwareRepository<T> : ITenantAwareRepository<T> where T : class, ITenantEntity
{
    protected readonly ICurrentTenant _currentTenant;
    
    protected TenantAwareRepository(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }
    
    public virtual async Task<List<T>> GetAllAsync()
    {
        var tenantId = _currentTenant.Id;
        return await GetByTenantAsync(tenantId);
    }
    
    protected abstract Task<List<T>> GetByTenantAsync(string tenantId);
}

public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

## Requirements

- .NET Standard 2.0+

## License

This project is part of the Making framework.