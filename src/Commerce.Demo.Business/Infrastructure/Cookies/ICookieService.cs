namespace Commerce.Demo.Business.Infrastructure.Cookies;

public interface ICookiesService
{
    string? Get(string? name);
    void Set(string name, string value, DateTime? expires = null);
    void Remove(string name);
    bool Contains(string name);
}
