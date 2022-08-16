using BricksAndHearts.Auth;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace BricksAndHearts.Services;

public interface IMailService
{
    public Task SendMsg(
        string body,
        string subject,
        List<string>? toAddresses,
        string fromName = "",
        string toName = ""
    );

    public void TrySendMsgInBackground(
        string body,
        string subject,
        List<string>? toAddresses = null
    );
}

public class MailService : IMailService
{
    private readonly IOptions<EmailConfigOptions> _config;
    private readonly ILogger<MailService> _logger;

    public MailService(IOptions<EmailConfigOptions> config, ILogger<MailService> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    public async Task SendMsg(
        string body,
        string subject,
        List<string>? toAddresses,
        string fromName = "",
        string toName = ""
    )
    {
        var fromAddress = _config.Value.FromAddress;
        toAddresses ??= _config.Value.ToAddress;
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };
        foreach (var address in toAddresses)
        {
            message.To.Add(new MailboxAddress(toName, address));
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_config.Value.Host, _config.Value.Port, true);

        // Note: only needed if the SMTP server requires authentication
        await client.AuthenticateAsync(_config.Value.UserName, _config.Value.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
    
    private async Task TrySendMsg(
        string body,
        string subject,
        List<string>? toAddresses,
        string fromName = "",
        string toName = ""
    )
    {
        try
        {
            await SendMsg(body, subject, toAddresses, fromName, toName);
            _logger.LogInformation("Successfully sent emails");
        }
        catch (Exception e)
        {
            // ignored
            _logger.LogWarning("Failed to send email with message:\n{Msg}\n \nSubject:\n{Subject}", body, subject);
            _logger.LogWarning("Email sending exception message: {E}", e);
        }
    }

    public void TrySendMsgInBackground(
        string body,
        string subject,
        List<string>? toAddresses = null
    )
    {
        Task.Run(() => TrySendMsg(body, subject, toAddresses));
    }
}