namespace PixelMate.Api.Mail;

public interface IMailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody, string textBody, CancellationToken ct = default);
}
