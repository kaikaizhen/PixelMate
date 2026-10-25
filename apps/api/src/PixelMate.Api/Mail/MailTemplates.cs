using System.Net;
using System.Reflection;

namespace PixelMate.Api.Mail;

/// <summary>
/// PixelMate-styled e-mails: a pixel banner with the star pet, a friendly line from the pet, one clear button,
/// a "good to know" box and a plain-text twin. Inline styles and tables only, so Gmail / Outlook / Apple Mail render alike.
/// Images are embedded (cid:) so they work without any public URL.
/// </summary>
public static class MailTemplates
{
    private const string Ink = "#4a3040", InkSoft = "#7a5f6c", Cream = "#fff8ec", Paper = "#fffdf8", Pink = "#fde2dd", Yellow = "#ffd86b";

    public static MailContent ConfirmEmail(string displayName, string link) => Build(new Layout(
        Subject: "✦ 小伴星在等你：請確認你的 Email",
        Banner: "banner-confirm",
        Preheader: "點一下按鈕，就能和你的小夥伴見面。",
        Title: "歡迎來到小伴星！",
        PetSays: "我是小伴星，我找到你了！",
        Body: "謝謝你加入。只差最後一步：點下面的按鈕，確認這個 Email 是你的，就可以登入囉。",
        Button: "確認我的 Email",
        Notes: ["這個連結 1 小時內有效，過期了可以在登入頁重新寄一封。", "沒有註冊過小伴星？直接忽略這封信就好，不會有任何事發生。"],
        DisplayName: displayName,
        Link: link));

    public static MailContent ResetPassword(string displayName, string link) => Build(new Layout(
        Subject: "✦ 小伴星：重新設定你的密碼",
        Banner: "banner-reset",
        Preheader: "別擔心，換一把新鑰匙就好。",
        Title: "忘記密碼沒關係",
        PetSays: "我幫你把鑰匙找回來了～",
        Body: "我們收到重新設定密碼的要求。點下面的按鈕，就能為你的小屋換一把新鑰匙。",
        Button: "設定新密碼",
        Notes: ["這個連結 1 小時內有效，而且只能使用一次。", "不是你申請的嗎？請忽略這封信，你的密碼不會改變。換了新密碼後，其他裝置會自動登出。"],
        DisplayName: displayName,
        Link: link));

    private sealed record Layout(string Subject, string Banner, string Preheader, string Title, string PetSays, string Body,
        string Button, string[] Notes, string DisplayName, string Link);

    private static MailContent Build(Layout l)
    {
        string E(string s) => WebUtility.HtmlEncode(s);
        var notes = string.Concat(l.Notes.Select(n => $"<tr><td style=\"padding:3px 0;font-size:13px;line-height:1.6;color:{InkSoft}\">✦ {E(n)}</td></tr>"));
        var html = $"""
            <!doctype html>
            <html lang="zh-Hant"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="color-scheme" content="light"><meta name="supported-color-schemes" content="light"><title>{E(l.Subject)}</title></head>
            <body style="margin:0;padding:0;background:{Cream};color:{Ink};font-family:-apple-system,'Segoe UI','PingFang TC','Noto Sans TC','Microsoft JhengHei',sans-serif">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0">{E(l.Preheader)}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Cream}"><tr><td align="center" style="padding:20px 12px">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:100%;max-width:600px">
                <tr><td style="border:3px solid {Ink};border-bottom:0;background:{Cream}"><img src="cid:{l.Banner}" width="594" alt="小伴星 PixelMate 的星星小夥伴" style="display:block;width:100%;height:auto;border:0;image-rendering:pixelated"></td></tr>
                <tr><td style="background:{Paper};border:3px solid {Ink};border-top:0;padding:28px 28px 20px">
                  <p style="margin:0 0 6px;font-size:12px;letter-spacing:3px;color:{InkSoft}">PIXELMATE</p>
                  <h1 style="margin:0 0 14px;font-size:24px;line-height:1.3;color:{Ink}">{E(l.Title)}</h1>
                  <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 16px"><tr>
                    <td style="background:{Pink};border:2px solid {Ink};border-radius:12px;padding:8px 14px;font-size:14px;color:{Ink}">💬 {E(l.PetSays)}</td></tr></table>
                  <p style="margin:0 0 6px;font-size:16px;line-height:1.7">嗨 {E(l.DisplayName)}，</p>
                  <p style="margin:0 0 22px;font-size:16px;line-height:1.7">{E(l.Body)}</p>
                  <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 22px"><tr>
                    <td align="center" bgcolor="{Yellow}" style="background:{Yellow};border:3px solid {Ink};border-bottom-width:7px;border-right-width:7px;border-radius:10px">
                      <a href="{E(l.Link)}" style="display:inline-block;padding:14px 30px;font-size:17px;font-weight:700;color:{Ink};text-decoration:none">{E(l.Button)} →</a></td></tr></table>
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Cream};border:2px dashed {Ink};border-radius:10px"><tr><td style="padding:12px 16px">
                    <p style="margin:0 0 4px;font-size:13px;font-weight:700">小提醒</p>
                    <table role="presentation" cellpadding="0" cellspacing="0">{notes}</table></td></tr></table>
                  <p style="margin:18px 0 0;font-size:12px;line-height:1.6;color:{InkSoft};word-break:break-all">按鈕沒反應？請把這串網址貼到瀏覽器：<br><a href="{E(l.Link)}" style="color:{InkSoft}">{E(l.Link)}</a></p>
                </td></tr>
                <tr><td style="background:{Pink};border:3px solid {Ink};border-top:0;padding:14px 20px">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
                    <td width="58" valign="middle"><img src="cid:mascot" width="48" alt="" style="display:block;border:0;image-rendering:pixelated"></td>
                    <td valign="middle" style="font-size:12px;line-height:1.6;color:{Ink}"><strong>小伴星 PixelMate</strong>　你的像素小夥伴<br>這是一封系統自動寄出的信，請不要直接回覆。</td></tr></table>
                </td></tr>
              </table>
            </td></tr></table></body></html>
            """;

        var text = $"""
            ✦ {l.Title} ✦

            小伴星說：「{l.PetSays}」

            嗨 {l.DisplayName}，

            {l.Body}

            {l.Button}：
            {l.Link}

            小提醒
            {string.Join("\n", l.Notes.Select(n => "✦ " + n))}

            —— 小伴星 PixelMate，你的像素小夥伴
            （這是一封系統自動寄出的信，請不要直接回覆。）
            """;
        return new MailContent(l.Subject, html, text, [Image(l.Banner), Image("mascot")]);
    }

    private static InlineImage Image(string name)
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"PixelMate.Api.Mail.Assets.{name}.png")
                      ?? throw new InvalidOperationException($"Mail image {name} is missing from the assembly.");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return new InlineImage(name, ms.ToArray());
    }
}
