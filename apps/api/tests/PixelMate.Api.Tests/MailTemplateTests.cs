using System.Text.RegularExpressions;
using PixelMate.Api.Mail;

namespace PixelMate.Api.Tests;

public class MailTemplateTests
{
    private const string Link = "https://example.test/web/reset-password?email=a%40b.co&token=T0k3n";

    [Fact]
    public void Confirm_mail_has_the_pixelmate_look_a_button_and_a_text_twin()
    {
        var m = MailTemplates.ConfirmEmail("小栗", Link);
        Assert.Contains("小伴星", m.Subject);
        Assert.Contains("確認我的 Email", m.Html);
        Assert.Contains(Link.Replace("&", "&amp;"), m.Html);        // the link is HTML-encoded in the href
        Assert.Contains(Link, m.Text);
        Assert.Contains("1 小時", m.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Every_cid_in_the_html_has_an_embedded_image(bool confirm)
    {
        var m = confirm ? MailTemplates.ConfirmEmail("小栗", Link) : MailTemplates.ResetPassword("小栗", Link);
        var used = Regex.Matches(m.Html, "cid:([a-z-]+)").Select(x => x.Groups[1].Value).Distinct().ToList();
        Assert.NotEmpty(used);
        Assert.All(used, id => Assert.Contains(m.Images, i => i.ContentId == id && i.Bytes.Length > 100));
        Assert.DoesNotContain("http://localhost", m.Html);          // no image may depend on a public URL
    }

    [Fact]
    public void User_supplied_text_is_html_encoded()
    {
        var m = MailTemplates.ResetPassword("<script>alert(1)</script>", Link);
        Assert.DoesNotContain("<script>", m.Html);
        Assert.Contains("&lt;script&gt;", m.Html);
    }

    [Fact]
    public void Reset_and_confirm_mails_are_different()
    {
        Assert.NotEqual(MailTemplates.ConfirmEmail("a", Link).Html, MailTemplates.ResetPassword("a", Link).Html);
        Assert.Contains("只能使用一次", MailTemplates.ResetPassword("a", Link).Html);
    }

    [Fact]
    public void Preview_files_are_written_when_asked()
    {
        var dir = Environment.GetEnvironmentVariable("PIXELMATE_MAIL_PREVIEW_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        foreach (var (name, m) in new[] { ("confirm", MailTemplates.ConfirmEmail("小栗", Link)), ("reset", MailTemplates.ResetPassword("小栗", Link)) })
        {
            var html = m.Images.Aggregate(m.Html, (h, i) => h.Replace($"cid:{i.ContentId}", $"data:image/png;base64,{Convert.ToBase64String(i.Bytes)}"));
            File.WriteAllText(Path.Combine(dir, name + ".html"), html);
        }
    }
}
