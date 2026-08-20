using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Solar.Infrastructure.Caching;

public class SolarMemoryCacheService : ISolarCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SolarMemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    public SolarMemoryCacheService(IMemoryCache cache, ILogger<SolarMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            _logger.LogDebug("[CACHE HIT] Chave: {CacheKey}", key);
            return cachedValue;
        }

        _logger.LogDebug("[CACHE MISS] Gerando valor para chave: {CacheKey}", key);
        var value = await factory();

        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromHours(1)
        };

        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _trackedKeys.TryRemove(evictedKey.ToString() ?? string.Empty, out _);
        });

        _cache.Set(key, value, entryOptions);
        _trackedKeys.TryAdd(key, 0);

        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        _logger.LogDebug("[CACHE INVALIDATED] Chave: {CacheKey}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _trackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in keysToRemove)
        {
            Remove(k);
        }
    }

    public void Clear()
    {
        foreach (var k in _trackedKeys.Keys.ToList())
        {
            Remove(k);
        }
    }
}
