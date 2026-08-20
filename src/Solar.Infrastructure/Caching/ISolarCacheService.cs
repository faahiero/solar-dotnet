namespace Solar.Infrastructure.Caching;

public interface ISolarCacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    void Clear();
}
