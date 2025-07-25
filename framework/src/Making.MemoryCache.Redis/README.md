# Making.MemoryCache.Redis

Redis-based memory cache implementation for the Making framework.

## Overview

Making.MemoryCache.Redis provides a distributed caching solution using Redis for the Making framework. It offers high-performance distributed caching with hybrid local/Redis caching, statistics tracking, and seamless scaling across multiple application instances.

## Features

- **Distributed Caching**: Redis-based distributed cache for multi-instance applications
- **Hybrid Caching**: Combines local memory cache with Redis for optimal performance
- **Cache Statistics**: Comprehensive performance metrics across all instances
- **Serialization**: Efficient JSON serialization for complex objects
- **Connection Management**: Robust Redis connection handling with retry logic
- **Pattern-based Operations**: Support for pattern-based cache invalidation

## Installation

```bash
dotnet add package Making.MemoryCache.Redis
```

## Usage

### Configuration

```json
{
  "Cache": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "mark:",
      "DefaultExpiration": "00:30:00",
      "LocalCacheEnabled": true,
      "LocalCacheExpiration": "00:05:00",
      "LocalCacheMaxSize": 1000
    }
  }
}
```

### Register Services

```csharp
services.AddMarkRedisCache(configuration);
```

### Basic Usage

```csharp
public class ProductService
{
    private readonly IMemoryCacheService _cache;
    
    public ProductService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<Product> GetProductAsync(int productId)
    {
        var cacheKey = $"product:{productId}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            // Expensive database operation
            return await _productRepository.GetByIdAsync(productId);
        }, TimeSpan.FromHours(1));
    }
    
    public async Task InvalidateProductAsync(int productId)
    {
        var cacheKey = $"product:{productId}";
        await _cache.RemoveAsync(cacheKey);
    }
}
```

### Hybrid Caching

```csharp
public class UserService
{
    private readonly IMemoryCacheService _cache;
    
    public UserService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<User> GetUserAsync(int userId)
    {
        var cacheKey = $"user:{userId}";
        
        // Hybrid cache automatically checks local cache first, then Redis
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user;
        }, TimeSpan.FromMinutes(30));
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
        
        // Invalidates both local and Redis cache
        var cacheKey = $"user:{user.Id}";
        await _cache.RemoveAsync(cacheKey);
        
        // Also invalidate related cache entries
        await _cache.RemoveByPatternAsync($"user:{user.Id}:*");
    }
}
```

### Distributed Cache Invalidation

```csharp
public class OrderService
{
    private readonly IMemoryCacheService _cache;
    
    public OrderService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        await _orderRepository.CreateAsync(order);
        
        // Invalidate user's order cache across all instances
        await _cache.RemoveByPatternAsync($"orders:user:{order.UserId}:*");
        
        // Invalidate product inventory cache
        foreach (var item in order.Items)
        {
            await _cache.RemoveAsync($"product:inventory:{item.ProductId}");
        }
    }
    
    public async Task<List<Order>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
    {
        var cacheKey = $"orders:user:{userId}:page:{page}:size:{pageSize}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _orderRepository.GetUserOrdersAsync(userId, page, pageSize);
        }, TimeSpan.FromMinutes(15));
    }
}
```

### Cache Statistics and Monitoring

```csharp
public class CacheMonitoringService
{
    private readonly IMemoryCacheService _cache;
    private readonly ILogger<CacheMonitoringService> _logger;
    
    public CacheMonitoringService(IMemoryCacheService cache, ILogger<CacheMonitoringService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<CacheHealthReport> GetCacheHealthAsync()
    {
        var stats = await _cache.GetStatisticsAsync();
        
        return new CacheHealthReport
        {
            IsHealthy = stats.HitRatio >= 0.7,
            HitRatio = stats.HitRatio,
            TotalRequests = stats.TotalRequests,
            ItemCount = stats.ItemCount,
            LocalCacheHits = stats.LocalCacheHits,
            RedisCacheHits = stats.RedisCacheHits
        };
    }
    
    public async Task LogDetailedStatistics()
    {
        var stats = await _cache.GetStatisticsAsync();
        
        _logger.LogInformation("Redis Cache Statistics: " +
            "Total Requests: {TotalRequests}, " +
            "Hit Ratio: {HitRatio:P2}, " +
            "Local Cache Hits: {LocalHits}, " +
            "Redis Cache Hits: {RedisHits}, " +
            "Cache Misses: {Misses}",
            stats.TotalRequests,
            stats.HitRatio,
            stats.LocalCacheHits,
            stats.RedisCacheHits,
            stats.MissCount);
    }
}
```

### Advanced Configuration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CacheOptions>(options =>
        {
            options.Redis.ConnectionString = "localhost:6379";
            options.Redis.Database = 1;
            options.Redis.KeyPrefix = "myapp:";
            options.Redis.DefaultExpiration = TimeSpan.FromMinutes(30);
            
            // Hybrid cache settings
            options.Redis.LocalCacheEnabled = true;
            options.Redis.LocalCacheExpiration = TimeSpan.FromMinutes(5);
            options.Redis.LocalCacheMaxSize = 2000;
            
            // Connection settings
            options.Redis.ConnectionTimeout = TimeSpan.FromSeconds(10);
            options.Redis.CommandTimeout = TimeSpan.FromSeconds(5);
            options.Redis.RetryCount = 3;
        });
        
        services.AddMarkRedisCache();
    }
}
```

### Cache Warming Strategy

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IMemoryCacheService _cache;
    private readonly IProductService _productService;
    
    public CacheWarmupService(IMemoryCacheService cache, IProductService productService)
    {
        _cache = cache;
        _productService = productService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Warm up frequently accessed data
            var categories = await _productService.GetCategoriesAsync();
            foreach (var category in categories)
            {
                var products = await _productService.GetProductsByCategoryAsync(category.Id);
                var cacheKey = $"products:category:{category.Id}";
                await _cache.SetAsync(cacheKey, products, TimeSpan.FromHours(2));
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail startup
            _logger.LogError(ex, "Cache warmup failed");
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Requirements

- .NET Standard 2.0+
- Redis Server
- StackExchange.Redis
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Options
- Microsoft.Extensions.Hosting.Abstractions
- System.Text.Json
- Making.Core
- Making.MemoryCache.Abstractions
- Making.MemoryCache

## License

This project is part of the Making framework.