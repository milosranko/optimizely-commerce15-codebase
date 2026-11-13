using Microsoft.AspNetCore.Http;

namespace Commerce.Demo.Business.Infrastructure.Cookies;

public class CookiesService : ICookiesService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookiesService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual string? Get(string? name)
    {
        if (_httpContextAccessor.HttpContext?.Request is null || string.IsNullOrEmpty(name))
            return default;

        else if (!_httpContextAccessor.HttpContext.Request.Cookies.ContainsKey(name))
        {
            var responseCookies = _httpContextAccessor.HttpContext.Response.GetTypedHeaders().SetCookie;
            var cookieValue = responseCookies.Where(x => x.Name.Equals(name) && x.Value.HasValue).FirstOrDefault()?.Value.Value;

            return string.IsNullOrEmpty(cookieValue)
                ? default
                : cookieValue;
        }

        return _httpContextAccessor.HttpContext.Request.Cookies[name];
    }

    public virtual void Set(string name, string value, DateTime? expires = null)
    {
        if (_httpContextAccessor.HttpContext is null)
            return;

        var cookieOptions = new CookieOptions
        {
            //HttpOnly = true,
            Secure = true,
            IsEssential = true,
            Path = "/"
        };

        if (expires.HasValue)
        {
            cookieOptions.Expires = expires.Value;
        }

        _httpContextAccessor.HttpContext.Response.Cookies.Append(name, value ?? string.Empty, cookieOptions);
    }

    public virtual void Remove(string name)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(name);
    }

    public virtual bool Contains(string name)
    {
        if (_httpContextAccessor.HttpContext?.Request is null)
            return false;

        return _httpContextAccessor.HttpContext.Request.Cookies.ContainsKey(name);
    }
}
