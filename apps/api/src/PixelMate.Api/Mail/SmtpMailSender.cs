using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PixelMate.Api.Mail;

/// <summary>Sends through SMTP with STARTTLS (Gmail: smtp.gmail.com:587). Failures are logged without addresses or secrets.</summary>
public sealed class SmtpMailSender(IOptions<MailOptions> options, ILogger<SmtpMailSender> log) : IMailSender
{
    /// <summary>
    /// Full chain validation, except that an *unreachable revocation list* (OCSP/CRL blocked by a firewall, or macOS
    /// not answering) does not fail the handshake. Every other certificate problem still rejects the connection.
    /// </summary>
    internal static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate? cert,
        System.Security.Cryptography.X509Certificates.X509Chain? chain, System.Net.Security.SslPolicyErrors errors)
    {
        if (errors == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }
        if (errors != System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors || chain is null)
        {
            return false;
        }
        return chain.ChainStatus.All(s => s.Status is System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError
            or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown
            or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation);
    }

    public async Task SendAsync(string toAddress, MailContent content, CancellationToken ct = default)
    {
        var o = options.Value;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(o.FromName, string.IsNullOrWhiteSpace(o.FromAddress) ? o.Username : o.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = content.Subject;
        var body = new BodyBuilder { HtmlBody = content.Html, TextBody = content.Text };
        foreach (var img in content.Images)
        {
            var part = body.LinkedResources.Add(img.ContentId + ".png", img.Bytes, ContentType.Parse(img.MediaType));
            part.ContentId = img.ContentId;
        }
        message.Body = body.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient { ServerCertificateValidationCallback = ValidateServerCertificate };
            await smtp.ConnectAsync(o.Host, o.Port, SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(o.Username, o.Password, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // never log the recipient, token or password; TLS errors carry no secrets and explain the real cause
            log.LogError("Mail delivery failed ({ExceptionType}){Reason}", ex.GetType().Name,
                ex is SslHandshakeException ? ": " + ex.Message : "");
        }
    }
}

/// <summary>Development / not-configured fallback: nothing is sent; the subject is logged so flows can be tested.</summary>
public sealed class LogMailSender(ILogger<LogMailSender> log, IHostEnvironment env) : IMailSender
{
    public Task SendAsync(string toAddress, MailContent content, CancellationToken ct = default)
    {
        if (env.IsDevelopment())
        {
            log.LogInformation("[dev mail] to={To} subject={Subject}\n{Text}", toAddress, content.Subject, content.Text);   // dev only: includes the link
        }
        else
        {
            log.LogWarning("Mail is not configured; the message \"{Subject}\" was not sent.", content.Subject);
        }
        return Task.CompletedTask;
    }
}
