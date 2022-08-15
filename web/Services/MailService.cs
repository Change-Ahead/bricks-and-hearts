using BricksAndHearts.Auth;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace BricksAndHearts.Services;

public interface IMailService
{
    public Task SendMsg(
        string msgBody,
        string subject,
        List<string>? msgToAddress,
        string msgFromName = "",
        string msgToName = ""
    );

    public void TrySendMsgInBackground(
        string msgBody,
        string subject,
        List<string>? msgToAddress
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
        string msgBody,
        string subject,
        List<string>? msgToAddress,
        string msgFromName = "",
        string msgToName = ""
    )
    {
        var msgFromAddress = _config.Value.FromAddress;
        msgToAddress ??= _config.Value.ToAddress;
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(msgFromName, msgFromAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = msgBody
        };
        foreach (var address in msgToAddress)
        {
            message.To.Add(new MailboxAddress(msgToName, address));
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_config.Value.Host, _config.Value.Port, true);

        // Note: only needed if the SMTP server requires authentication
        await client.AuthenticateAsync(_config.Value.UserName, _config.Value.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
    
    private async Task TrySendMsg(
        string msgBody,
        string subject,
        List<string>? msgToAddress,
        string msgFromName = "",
        string msgToName = ""
    )
    {
        try
        {
            await SendMsg(msgBody, subject, msgToAddress, msgFromName, msgToName);
            _logger.LogInformation("Successfully sent emails");
        }
        catch (Exception e)
        {
            // ignored
            _logger.LogWarning("Failed to send email with message:\n{Msg}\n \nSubject:\n{Subject}", msgBody, subject);
            _logger.LogWarning("Email sending exception message: {E}", e);
        }
    }

    public void TrySendMsgInBackground(
        string msgBody,
        string subject,
        List<string>? msgToAddress
    )
    {
        Task.Run(() => TrySendMsg(msgBody, subject, msgToAddress));
    }
}