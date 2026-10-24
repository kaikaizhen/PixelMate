using System.Net;

namespace PixelMate.Api.Mail;

public static class MailTemplates
{
    public static (string Subject, string Html, string Text) ConfirmEmail(string displayName, string link) =>
        Build("確認你的小伴星帳號", displayName, "謝謝你加入小伴星！請點下面的連結確認你的 Email，連結 1 小時內有效。", "確認 Email", link,
            "如果這不是你本人操作，請直接忽略這封信。");

    public static (string Subject, string Html, string Text) ResetPassword(string displayName, string link) =>
        Build("重新設定小伴星的密碼", displayName, "我們收到重新設定密碼的要求。請點下面的連結設定新密碼，連結 1 小時內有效，只能使用一次。", "重新設定密碼", link,
            "如果不是你本人申請，請忽略這封信，你的密碼不會改變。");

    private static (string, string, string) Build(string subject, string name, string intro, string button, string link, string footer)
    {
        var n = WebUtility.HtmlEncode(name);
        var l = WebUtility.HtmlEncode(link);
        var html = $"""
            <div style="font-family:system-ui,'Noto Sans TC',sans-serif;max-width:480px;margin:auto;color:#4a3040">
              <h2 style="margin:0 0 12px">{WebUtility.HtmlEncode(subject)}</h2>
              <p>嗨 {n}，</p><p>{WebUtility.HtmlEncode(intro)}</p>
              <p><a href="{l}" style="display:inline-block;background:#e9a98e;color:#3a2433;padding:10px 18px;border-radius:8px;text-decoration:none">{WebUtility.HtmlEncode(button)}</a></p>
              <p style="word-break:break-all;font-size:12px;color:#8a7080">若按鈕無法使用，請複製這個網址：<br>{l}</p>
              <p style="font-size:12px;color:#8a7080">{WebUtility.HtmlEncode(footer)}</p>
            </div>
            """;
        var text = $"嗨 {name}，\n\n{intro}\n\n{button}：{link}\n\n{footer}\n";
        return (subject, html, text);
    }
}
