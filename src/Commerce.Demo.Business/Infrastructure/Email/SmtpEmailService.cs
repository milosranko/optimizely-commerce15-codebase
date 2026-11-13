using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Demo.Business.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        await SendAsync([to], subject, htmlBody, ct);
    }

    public async Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken ct = default)
    {
        var recipientList = recipients.ToList();
        if (recipientList.Count == 0)
        {
            _logger.LogWarning("SendAsync called with no recipients for subject: {Subject}", subject);
            return;
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_options.DefaultFromAddress, _options.DefaultFromDisplayName);
        foreach (var recipient in recipientList)
        {
            message.To.Add(recipient);
        }
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_options.Host, _options.Port);
        if (!string.IsNullOrEmpty(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }
        client.EnableSsl = _options.Port != 25;

        try
        {
            _logger.LogInformation("Sending email to {Recipients} with subject: {Subject}",
                string.Join(", ", recipientList), subject);

            await client.SendMailAsync(message, ct);

            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", recipientList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}",
                string.Join(", ", recipientList), subject);
            throw;
        }
    }
}
