using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PixelMate.Api.Mail;

/// <summary>Sends through SMTP with STARTTLS (Gmail: smtp.gmail.com:587). Failures are logged without addresses or secrets.</summary>
public sealed class SmtpMailSender(IOptions<MailOptions> options, ILogger<SmtpMailSender> log) : IMailSender
{
    public async Task SendAsync(string toAddress, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        var o = options.Value;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(o.FromName, string.IsNullOrWhiteSpace(o.FromAddress) ? o.Username : o.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(o.Host, o.Port, SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(o.Username, o.Password, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            log.LogError("Mail delivery failed ({ExceptionType})", ex.GetType().Name);   // never log the recipient, token or password
        }
    }
}

/// <summary>Development / not-configured fallback: nothing is sent; the subject is logged so flows can be tested.</summary>
public sealed class LogMailSender(ILogger<LogMailSender> log, IHostEnvironment env) : IMailSender
{
    public Task SendAsync(string toAddress, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        if (env.IsDevelopment())
        {
            log.LogInformation("[dev mail] to={To} subject={Subject}\n{Text}", toAddress, subject, textBody);   // dev only: includes the link
        }
        else
        {
            log.LogWarning("Mail is not configured; the message \"{Subject}\" was not sent.", subject);
        }
        return Task.CompletedTask;
    }
}
