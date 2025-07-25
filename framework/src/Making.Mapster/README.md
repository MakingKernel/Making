# Making.Mapster

Mapster integration and extensions for the Making framework.

## Overview

Making.Mapster provides seamless integration with Mapster object mapping library for the Making framework. It offers dependency injection setup, configuration extensions, and optimized mapping performance for domain objects, DTOs, and entities.

## Features

- **Mapster Integration**: Easy Mapster configuration and setup
- **Dependency Injection**: Full DI container integration
- **Performance Optimized**: Pre-compiled mapping expressions
- **Configuration Extensions**: Fluent mapping configuration
- **Type Adapters**: Custom type adapters and converters

## Installation

```bash
dotnet add package Making.Mapster
```

## Usage

### Register Services

```csharp
services.AddMakingMapster();
```

### Basic Mapping

```csharp
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public async Task<UserDto> GetUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return _mapper.Map<UserDto>(user);
    }
    
    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = _mapper.Map<User>(request);
        await _userRepository.CreateAsync(user);
        
        return user;
    }
}
```

### Custom Mapping Configuration

```csharp
public class MappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Basic mapping
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Age, src => DateTime.Now.Year - src.BirthYear);
        
        // Ignore properties
        config.NewConfig<CreateUserRequest, User>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt);
        
        // Complex mapping
        config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.TotalAmount, src => src.Items.Sum(i => i.Price * i.Quantity))
            .Map(dest => dest.ItemCount, src => src.Items.Count);
    }
}

// Register mapping profile
services.AddMakingMapster(config =>
{
    config.Scan(typeof(MappingProfile).Assembly);
});
```

### Collection Mapping

```csharp
public class ProductService
{
    private readonly IMapper _mapper;
    
    public ProductService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public async Task<List<ProductDto>> GetProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return _mapper.Map<List<ProductDto>>(products);
    }
    
    public async Task<PagedResult<ProductDto>> GetPagedProductsAsync(int page, int size)
    {
        var products = await _productRepository.GetPagedAsync(page, size);
        var totalCount = await _productRepository.GetCountAsync();
        
        return new PagedResult<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = size
        };
    }
}
```

## Requirements

- .NET Standard 2.0+
- Mapster
- Microsoft.Extensions.DependencyInjection.Abstractions

## License

This project is part of the Making framework.