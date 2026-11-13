namespace Commerce.Demo.Business.Infrastructure.Email;

public class EmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string DefaultFromAddress { get; set; } = "noreply@domain.com";
    public string DefaultFromDisplayName { get; set; } = "Commerce Demo";
}
