namespace PixelMate.Api.Mail;

/// <summary>Outgoing mail (Gmail SMTP). Values come from environment variables / user-secrets, never the repo.</summary>
public sealed class MailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";              // a Gmail *app password*, not the account password
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "小伴星 PixelMate";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
