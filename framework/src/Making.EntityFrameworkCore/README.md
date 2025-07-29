# Making.EntityFrameworkCore

Professional Entity Framework Core integration for the Making framework, implementing Unit of Work and Repository patterns with domain events support.

## Key Features

- **Domain-Driven Design Integration**: Full support for domain entities, aggregates, and domain events
- **Unit of Work Pattern**: Comprehensive transaction management with EfCoreUnitOfWork
- **Repository Pattern**: Generic repositories with async operations and LINQ support
- **Domain Events**: Automatic domain event publishing during SaveChanges
- **Multi-tenancy Support**: Built-in multi-tenant entity handling
- **Soft Delete**: Automatic soft delete implementation
- **Auditing**: Creation and modification time tracking
- **Transaction Management**: Advanced transaction control with rollback support

## Architecture

### MakingDbContext
Base DbContext that extends Entity Framework Core with:
- Domain events publishing
- Multi-tenancy concepts
- Soft delete handling
- Auditing support
- Change tracking enhancements

### EfCoreUnitOfWork
Professional implementation of IUnitOfWork with:
- Transaction lifecycle management
- Automatic change detection
- Error handling and rollback
- Async operations support

### Generic Repositories
Type-safe repositories implementing:
- Standard CRUD operations
- LINQ query support
- Bulk operations
- Async/await patterns

## Usage Example

```csharp
// Service registration
services.AddMakingEntityFrameworkCore<MyAppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Custom DbContext
public class MyAppDbContext : MakingDbContext
{
    public MyAppDbContext(DbContextOptions<MyAppDbContext> options, IServiceProvider serviceProvider) 
        : base(options, serviceProvider)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}

// Domain entity with events
public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public void UpdatePrice(decimal newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        
        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }
}
```

## Professional Design Principles

1. **Separation of Concerns**: Clear separation between domain, infrastructure, and application layers
2. **SOLID Principles**: Interfaces, dependency injection, and extensibility
3. **Domain Events**: Proper event-driven architecture support
4. **Error Handling**: Comprehensive exception handling and logging
5. **Performance**: Efficient change tracking and bulk operations
6. **Testability**: Mockable interfaces and dependency injection support