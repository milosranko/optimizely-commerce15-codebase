using EPiServer;
using EPiServer.Framework.Cache;

namespace Commerce.Demo.Business.Infrastructure.Caching;

/// <summary>
/// Cache wrapper over Optimizely's <see cref="ISynchronizedObjectInstanceCache"/> (CMS 13 removed the
/// static CacheManager). Removals are synchronised across instances; all expiry is absolute.
/// </summary>
public class CacheService : ICacheService
{
    private static readonly IList<string> Keys = [];

    private readonly ISynchronizedObjectInstanceCache _cache;

    public CacheService(ISynchronizedObjectInstanceCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Checks that the item is not null and IF the object implements IHasSuccessStatus the success flag must be set to true. 
    /// This is used to avoid caching empty objects or failed responses
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <returns></returns>
    private static bool ItemShouldBeCached<T>(T? item) where T : class
    {
        var shouldBeCached = item != null;
        return shouldBeCached;
    }

    public T? Get<T>(string key) where T : class
    {
        var item = _cache.Get(key);
        return (T)item;
    }

    public void Insert<T>(string key, T? item) where T : class
    {
        Insert(key, item, CacheServiceSettings.DefaultCacheTime);
    }

    public void Insert<T>(string key, T? item, TimeSpan expiresBy) where T : class
    {
        if (item is null)
            return;

        _cache.Insert(key, item, new CacheEvictionPolicy(expiresBy, CacheTimeoutType.Absolute));
        Keys.Add(key);
    }

    /// <summary>
    /// Gets or sets the value in cache for up to default cache time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="getT"></param>
    /// <returns></returns>
    public T? GetOrSet<T>(string key, Func<T?> getT) where T : class
    {
        return GetOrSet(key, getT, CacheServiceSettings.DefaultCacheTime);
    }

    public Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getT) where T : class
    {
        return GetOrSetAsync(key, getT, CacheServiceSettings.DefaultCacheTime);
    }

    /// <summary>
    /// Gets or sets the value in cache. Use expiresBy to add max duration in cache.
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="key"></param>
    /// <param name="getT"></param>
    /// <param name="expiresBy"></param>
    /// <returns></returns>
    public T? GetOrSet<T>(string key, Func<T?> getT, TimeSpan expiresBy) where T : class
    {
        var cacheObject = _cache.Get(key);

        if (cacheObject != null)
        {
            var cacheItem = (T)cacheObject;
            return cacheItem;
        }

        var cacheItemFresh = getT.Invoke();

        if (ItemShouldBeCached(cacheItemFresh))
        {
            Insert(key, cacheItemFresh, expiresBy);
        }

        return cacheItemFresh;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getT, TimeSpan expiresBy) where T : class
    {
        var cacheObject = _cache.Get(key);

        if (cacheObject != null)
        {
            var cacheItem = (T)cacheObject;
            return cacheItem;
        }

        var cacheItemFresh = await getT.Invoke();

        if (ItemShouldBeCached(cacheItemFresh))
        {
            Insert(key, cacheItemFresh, expiresBy);
        }

        return cacheItemFresh;
    }

    public object Get(string key)
    {
        return _cache.Get(key);
    }

    /// <summary>
    /// Remove the cached item equal to the key.
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key)
    {
        if (!Keys.Contains(key))
            return;

        _cache.Remove(key);
        Keys.Remove(key);
    }

    public void Clear()
    {
        foreach (var item in Keys)
            _cache.RemoveLocal(item);

        Keys.Clear();
    }

    /// <summary>
    /// Remove the cached items beginning with the key
    /// </summary>
    /// <param name="startsWith"></param>
    public void RemoveStartsWith(string startsWith)
    {
        var keys = Keys.Where(k => k.StartsWith(startsWith)).ToList();

        foreach (var k in keys)
            Remove(k);
    }

    [Obsolete("Use strongly typed variants")]
    public void Insert(string key, object value, CacheEvictionPolicy evictionPolicy)
    {
        throw new NotImplementedException();
    }
}

public class CacheServiceSettings
{
    public static TimeSpan DefaultCacheTime = TimeSpan.FromMinutes(5);
}
