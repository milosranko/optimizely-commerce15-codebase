namespace Commerce.Demo.Business.Infrastructure.Caching;

public abstract class CacheDecoratorBase
{
    protected ICacheService CacheService { get; }

    protected CacheDecoratorBase(ICacheService cacheService)
    {
        CacheService = cacheService;
    }

    protected abstract string CacheKey { get; }

    public virtual void RemoveFromCache(string? cacheKey = null)
    {
        cacheKey ??= CacheKey;

        CacheService.Remove(cacheKey);
    }
}
