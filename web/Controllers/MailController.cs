using BricksAndHearts.Auth;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BricksAndHearts.Controllers;

[AllowAnonymous]
public class MailController : Controller
{
    private readonly IOptions<EmailConfigOptions> _config;

    public MailController(IOptions<EmailConfigOptions> config)
    {
        _config = config;
    }

    public IActionResult SendMsg(
        string msgFromAddress = "tech@changeahead.org.uk",
        string msgToAddress = "alice.luo@softwire.com",
        string msgBody = "Hi",
        string subject = "From Softwire Intern Alice",
        string msgFromName = "",
        string msgToName = ""
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(msgFromName, msgFromAddress));
        message.To.Add(new MailboxAddress(msgToName, msgToAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = msgBody
        };
        using (var client = new SmtpClient())
        {
            client.Connect("mail.epclients.co.uk", 465, true);

            // Note: only needed if the SMTP server requires authentication
            client.Authenticate(_config.Value.UserName, _config.Value.Password);

            client.Send(message);
            client.Disconnect(true);
        }
        return RedirectToAction(nameof(Index),"Home");
    }
}