namespace Commerce.Demo.Web.Models;

public record LinkViewModel(string Text, string Url, bool IsExternal, string? Target = null);
