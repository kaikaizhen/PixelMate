namespace PixelMate.Api.Mail;

/// <summary>An image embedded in the message and referenced from the HTML as <c>cid:ContentId</c>.</summary>
public sealed record InlineImage(string ContentId, byte[] Bytes, string MediaType = "image/png");

/// <summary>A ready-to-send message: subject, HTML, a plain-text alternative and the images the HTML uses.</summary>
public sealed record MailContent(string Subject, string Html, string Text, IReadOnlyList<InlineImage> Images);

public interface IMailSender
{
    Task SendAsync(string toAddress, MailContent content, CancellationToken ct = default);
}
