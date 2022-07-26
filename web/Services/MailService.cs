using System.Net.Mail;
using BricksAndHearts.Auth;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace BricksAndHearts.Services;

public interface IMailService
{
    public Task SendMsg(
        string msgBody,
        string subject,
        string msgFromName = "",
        string msgToName = ""
    );
}

public class MailService : IMailService
{
    private readonly IOptions<EmailConfigOptions> _config;

    public MailService(IOptions<EmailConfigOptions> config)
    {
        _config = config;
    }
    
    public async Task SendMsg(
        string msgBody,
        string subject,
        string msgFromName = "",
        string msgToName = ""
    )
    {
        var msgFromAddress = _config.Value.FromAddress; 
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(msgFromName, msgFromAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = msgBody
        };
        foreach (var msgToAddress in _config.Value.ToAddress)
        {
            message.To.Add(new MailboxAddress(msgToName, msgToAddress));
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_config.Value.Host, _config.Value.Port, true);

        // Note: only needed if the SMTP server requires authentication
        await client.AuthenticateAsync(_config.Value.UserName, _config.Value.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}