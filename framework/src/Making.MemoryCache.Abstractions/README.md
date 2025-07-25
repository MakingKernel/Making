# Making.MemoryCache.Abstractions

Memory cache abstractions and interfaces for the Making framework.

## Overview

Making.MemoryCache.Abstractions provides the core abstractions and interfaces for memory caching in the Making framework. It defines contracts for cache services, statistics, and models that can be implemented by various caching providers.

## Features

- **Cache Service Interface**: Core caching service abstraction
- **Cache Statistics**: Performance metrics and cache hit/miss tracking
- **Cache Models**: Common cache entry and metadata models
- **Provider Abstraction**: Abstraction layer for different cache implementations
- **Async Support**: Full async/await support for cache operations

## Installation

```bash
dotnet add package Making.MemoryCache.Abstractions
```

## Usage

### Cache Service Interface

```csharp
public interface IMemoryCacheService
{
    Task<T> GetAsync<T>(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAsync();
    Task<CacheStatistics> GetStatisticsAsync();
    Task<bool> ExistsAsync(string key);
}
```

### Implementing Custom Cache Provider

```csharp
public class CustomCacheService : IMemoryCacheService
{
    public async Task<T> GetAsync<T>(string key)
    {
        // Custom implementation
        throw new NotImplementedException();
    }
    
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var item = await GetAsync<T>(key);
        if (item != null)
        {
            return item;
        }
        
        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Custom implementation
        throw new NotImplementedException();
    }
    
    public async Task RemoveAsync(string key)
    {
        // Custom implementation
        throw new NotImplementedException();
    }
    
    public async Task RemoveByPatternAsync(string pattern)
    {
        // Custom implementation
        throw new NotImplementedException();
    }
    
    public async Task ClearAsync()
    {
        // Custom implementation
        throw new NotImplementedException();
    }
    
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return new CacheStatistics
        {
            HitCount = 100,
            MissCount = 20,
            TotalRequests = 120,
            HitRatio = 0.83,
            ItemCount = 50
        };
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        var item = await GetAsync<object>(key);
        return item != null;
    }
}
```

### Cache Models

```csharp
public class CacheStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests { get; set; }
    public double HitRatio { get; set; }
    public long ItemCount { get; set; }
    public DateTime LastResetTime { get; set; }
}

public class CacheEntry<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
}
```

### Using in Services

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
            return await _repository.GetProductAsync(productId);
        }, TimeSpan.FromMinutes(30));
    }
    
    public async Task InvalidateProductCache(int productId)
    {
        var cacheKey = $"product:{productId}";
        await _cache.RemoveAsync(cacheKey);
    }
    
    public async Task<CacheStatistics> GetCachePerformance()
    {
        return await _cache.GetStatisticsAsync();
    }
}
```

## Requirements

- .NET Standard 2.0+

## License

This project is part of the Making framework.