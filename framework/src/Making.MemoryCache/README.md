# Making.MemoryCache

In-memory cache implementation for the Making framework.

## Overview

Making.MemoryCache provides a robust in-memory caching implementation for the Making framework. It offers high-performance caching with statistics tracking, expiration policies, and seamless integration with the Making caching abstractions.

## Features

- **In-Memory Caching**: Fast, local memory-based caching
- **Statistics Tracking**: Comprehensive cache performance metrics
- **Expiration Policies**: Sliding and absolute expiration support
- **Pattern-based Removal**: Remove cache entries by key patterns
- **Thread-Safe**: Concurrent access support
- **JSON Serialization**: Automatic serialization for complex types

## Installation

```bash
dotnet add package Making.MemoryCache
```

## Usage

### Register Services

```csharp
services.AddMarkMemoryCache();
```

### Basic Caching

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
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            // Fetch from database
            return await _userRepository.GetByIdAsync(userId);
        }, TimeSpan.FromMinutes(30));
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
        
        // Invalidate cache
        var cacheKey = $"user:{user.Id}";
        await _cache.RemoveAsync(cacheKey);
    }
}
```

### Advanced Caching Scenarios

```csharp
public class ProductService
{
    private readonly IMemoryCacheService _cache;
    
    public ProductService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        var cacheKey = $"products:category:{categoryId}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _productRepository.GetByCategoryAsync(categoryId);
        }, TimeSpan.FromHours(1));
    }
    
    public async Task InvalidateCategoryCache(int categoryId)
    {
        // Remove all cache entries for this category
        await _cache.RemoveByPatternAsync($"products:category:{categoryId}*");
    }
    
    public async Task ClearAllProductCache()
    {
        // Remove all product-related cache entries
        await _cache.RemoveByPatternAsync("products:*");
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
    
    public async Task LogCacheStatistics()
    {
        var stats = await _cache.GetStatisticsAsync();
        
        _logger.LogInformation("Cache Statistics: " +
            "Hit Count: {HitCount}, " +
            "Miss Count: {MissCount}, " +
            "Hit Ratio: {HitRatio:P2}, " +
            "Total Items: {ItemCount}",
            stats.HitCount,
            stats.MissCount,
            stats.HitRatio,
            stats.ItemCount);
    }
    
    public async Task<bool> IsCacheHealthy()
    {
        var stats = await _cache.GetStatisticsAsync();
        
        // Consider cache healthy if hit ratio is above 70%
        return stats.HitRatio >= 0.7;
    }
}
```

### Configuration Options

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMarkMemoryCache(options =>
        {
            options.DefaultExpiration = TimeSpan.FromMinutes(30);
            options.MaxItemCount = 10000;
            options.CompactionPercentage = 0.1; // Remove 10% when limit reached
            options.ScanFrequency = TimeSpan.FromMinutes(5);
        });
    }
}
```

### Custom Serialization

```csharp
public class ComplexDataService
{
    private readonly IMemoryCacheService _cache;
    
    public ComplexDataService(IMemoryCacheService cache)
    {
        _cache = cache;
    }
    
    public async Task<ComplexObject> GetComplexDataAsync(string id)
    {
        var cacheKey = $"complex:{id}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            // Complex computation or external API call
            var data = await _externalService.GetDataAsync(id);
            
            return new ComplexObject
            {
                Id = id,
                Data = data,
                ProcessedAt = DateTime.UtcNow
            };
        }, TimeSpan.FromHours(2));
    }
}
```

### Cache Warming

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
        // Warm up cache with frequently accessed data
        var popularProducts = await _productService.GetPopularProductsAsync();
        
        foreach (var product in popularProducts)
        {
            var cacheKey = $"product:{product.Id}";
            await _cache.SetAsync(cacheKey, product, TimeSpan.FromHours(1));
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Requirements

- .NET Standard 2.0+
- Microsoft.Extensions.Caching.Memory
- Microsoft.Extensions.Logging.Abstractions
- System.Text.Json
- Making.Core
- Making.MemoryCache.Abstractions

## License

This project is part of the Making framework.