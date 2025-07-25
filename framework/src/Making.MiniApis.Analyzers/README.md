# Making.MiniApis.Analyzers

Code analyzers and source generators for Making Mini APIs.

## Overview

Making.MiniApis.Analyzers provides powerful Roslyn-based source generators and code analyzers for building lightweight APIs with the Making framework. It automatically generates API endpoints, route mappings, and dependency injection registration code, eliminating boilerplate and ensuring consistency.

## Features

- **Mini API Generation**: Automatically generates API endpoints from controller classes
- **Route Analysis**: Intelligent route analysis and mapping
- **Dependency Injection**: Automatic DI registration for API controllers
- **Template Engine**: Customizable code generation templates
- **Symbol Analysis**: Deep analysis of types and methods for code generation
- **Performance Optimized**: Compile-time code generation for runtime performance

## Installation

```bash
dotnet add package Making.MiniApis.Analyzers
```

## Usage

### Define Mini API Controllers

```csharp
[MiniApi]
public class UserController
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("/api/users")]
    public async Task<IResult> GetUsers([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var users = await _userService.GetUsersAsync(page, size);
        return Results.Ok(users);
    }
    
    [HttpGet("/api/users/{id:int}")]
    public async Task<IResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
    
    [HttpPost("/api/users")]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return Results.Created($"/api/users/{user.Id}", user);
    }
    
    [HttpPut("/api/users/{id:int}")]
    public async Task<IResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        await _userService.UpdateUserAsync(id, request);
        return Results.NoContent();
    }
    
    [HttpDelete("/api/users/{id:int}")]
    public async Task<IResult> DeleteUser(int id)
    {
        await _userService.DeleteUserAsync(id);
        return Results.NoContent();
    }
}
```

### Generated Extension Methods

The analyzer automatically generates extension methods for API registration:

```csharp
// Generated code (you don't need to write this)
public static class UserControllerExtensions
{
    public static IEndpointRouteBuilder MapUserController(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/users", 
            async (IUserService userService, int page = 1, int size = 10) =>
            {
                var controller = new UserController(userService);
                return await controller.GetUsers(page, size);
            });
            
        endpoints.MapGet("/api/users/{id:int}", 
            async (IUserService userService, int id) =>
            {
                var controller = new UserController(userService);
                return await controller.GetUser(id);
            });
            
        endpoints.MapPost("/api/users", 
            async (IUserService userService, CreateUserRequest request) =>
            {
                var controller = new UserController(userService);
                return await controller.CreateUser(request);
            });
            
        // ... other endpoints
        
        return endpoints;
    }
}

// Main extensions class
public static class MiniApiExtensions
{
    public static IEndpointRouteBuilder MapMarkMiniApis(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserController();
        endpoints.MapProductController();
        endpoints.MapOrderController();
        // ... other controllers
        
        return endpoints;
    }
}
```

### Using Generated APIs

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add services
        builder.Services.AddMarkServices();
        
        var app = builder.Build();
        
        // Map all Mini APIs
        app.MapMarkMiniApis();
        
        app.Run();
    }
}
```

### Advanced Scenarios

```csharp
[MiniApi("api/v1")]
public class ProductController
{
    private readonly IProductService _productService;
    
    public ProductController(IProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), 200)]
    public async Task<IResult> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var products = await _productService.SearchProductsAsync(search, page, size);
        return Results.Ok(products);
    }
    
    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IResult> GetProduct(Guid id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            return Results.Ok(product);
        }
        catch (ProductNotFoundException)
        {
            return Results.NotFound($"Product with ID {id} not found");
        }
    }
    
    [HttpPost("products")]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!request.IsValid())
        {
            return Results.ValidationProblem(request.GetValidationErrors());
        }
        
        var product = await _productService.CreateProductAsync(request);
        return Results.Created($"api/v1/products/{product.Id}", product);
    }
}
```

### Custom Route Templates

```csharp
[MiniApi]
public class OrderController
{
    [HttpGet("/orders")]
    [RouteTemplate("GetOrders")]
    public async Task<IResult> GetOrders([FromQuery] OrderFilter filter)
    {
        // Implementation
    }
    
    [HttpGet("/orders/{orderId}/items")]
    [RouteTemplate("GetOrderItems")]  
    public async Task<IResult> GetOrderItems(string orderId)
    {
        // Implementation
    }
}
```

### Middleware Integration

```csharp
[MiniApi]
[Authorize] // Applies to all endpoints
public class AdminController
{
    [HttpGet("/admin/users")]
    [RequireRole("Admin")]
    public async Task<IResult> GetAllUsers([FromServices] IUserService userService)
    {
        var users = await userService.GetAllUsersAsync();
        return Results.Ok(users);
    }
    
    [HttpPost("/admin/users/{id}/ban")]
    [RequireRole("SuperAdmin")]
    public async Task<IResult> BanUser(int id, [FromServices] IUserService userService)
    {
        await userService.BanUserAsync(id);
        return Results.NoContent();
    }
}
```

### Configuration Options

Create a `mark.miniapis.json` file:

```json
{
  "generateControllerExtensions": true,
  "generateMainExtension": true,
  "routePrefix": "api",
  "generateSwaggerAnnotations": true,
  "generateValidation": true,
  "templateDirectory": "./Templates",
  "outputNamespace": "MyApp.Generated.MiniApis",
  "generateAsyncMethods": true
}
```

### Custom Templates

Override default templates by creating custom T4 templates:

```csharp
// Templates/CustomMethodMapping.tt
<#@ template language="C#" #>
<#@ parameter name="Method" type="MethodInfo" #>
endpoints.Map<#= Method.HttpMethod #>("<#= Method.Route #>", 
    async (<#= Method.Parameters #>) =>
    {
        // Custom logic here
        var controller = new <#= Method.ControllerName #>(<#= Method.Dependencies #>);
        return await controller.<#= Method.Name #>(<#= Method.Arguments #>);
    })
    <#= Method.Constraints #>
    <#= Method.Metadata #>;
```

## Requirements

- .NET Standard 2.0+
- Microsoft.CodeAnalysis.Analyzers
- Microsoft.CodeAnalysis.CSharp
- C# 9.0+ (for source generators)
- ASP.NET Core 6.0+

## License

This project is part of the Making framework.