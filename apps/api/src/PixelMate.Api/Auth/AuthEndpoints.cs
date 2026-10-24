using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Mail;

namespace PixelMate.Api.Auth;

public sealed class AuthOptions
{
    /// <summary>Block sign-in until the Email link was used. Turn on once mail is configured.</summary>
    public bool RequireConfirmedEmail { get; set; }
    /// <summary>Public base URL of the web app, used to build links in e-mails.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuth(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/auth").RequireRateLimiting("auth");

        g.MapPost("/register", Register).Validate<RegisterRequest>();
        g.MapPost("/login", Login).Validate<LoginRequest>();
        g.MapPost("/logout", Logout).RequireAuthorization();
        g.MapGet("/me", Me).RequireAuthorization();
        g.MapPost("/confirm-email", ConfirmEmail).Validate<ConfirmEmailRequest>();
        g.MapPost("/resend-confirmation", ResendConfirmation).Validate<EmailRequest>();
        g.MapPost("/forgot-password", ForgotPassword).Validate<EmailRequest>();
        g.MapPost("/reset-password", ResetPassword).Validate<ResetPasswordRequest>();
        return api;
    }

    private static UserDto ToDto(AppUser u) => new(u.Id, u.Email ?? "", u.DisplayName, u.EmailConfirmed);

    private static string Link(AuthOptions o, string path, params (string Key, string Value)[] query) =>
        $"{o.WebBaseUrl.TrimEnd('/')}{path}?" + string.Join('&', query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));

    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? Decode(string token)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return null; }
    }

    private static async Task SendConfirmation(AppUser user, UserManager<AppUser> users, IMailSender mail, IOptions<AuthOptions> opt, CancellationToken ct)
    {
        var token = Encode(await users.GenerateEmailConfirmationTokenAsync(user));
        var (subject, html, text) = MailTemplates.ConfirmEmail(user.DisplayName,
            Link(opt.Value, "/confirm-email", ("userId", user.Id.ToString()), ("token", token)));
        await mail.SendAsync(user.Email!, subject, html, text, ct);
    }

    private static async Task<IResult> Register(RegisterRequest req, UserManager<AppUser> users, IMailSender mail,
        IOptions<AuthOptions> opt, HttpContext ctx, CancellationToken ct)
    {
        var email = req.Email.Trim();
        var user = new AppUser { UserName = email, Email = email, DisplayName = req.DisplayName.Trim(), CreatedAt = DateTime.UtcNow };
        var result = await users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return Results.Json(new ApiError(ErrorCodes.EmailTaken, "這個 Email 已經註冊過了。", ctx.TraceIdentifier), statusCode: 409);
            }
            var errors = new Dictionary<string, string[]> { ["password"] = result.Errors.Select(e => e.Description).ToArray() };
            return Results.Json(new ApiError(ErrorCodes.ValidationFailed, "請檢查輸入的內容。", ctx.TraceIdentifier, errors), statusCode: 422);
        }

        await SendConfirmation(user, users, mail, opt, ct);
        return Results.Created($"/api/v1/auth/me", new { userId = user.Id, emailConfirmationRequired = opt.Value.RequireConfirmedEmail });
    }

    private static async Task<IResult> Login(LoginRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn,
        IOptions<AuthOptions> opt, HttpContext ctx)
    {
        var user = await users.FindByEmailAsync(req.Email.Trim());
        var invalid = Results.Json(new ApiError(ErrorCodes.AuthRequired, "Email 或密碼不正確。", ctx.TraceIdentifier), statusCode: 401);
        if (user is null)
        {
            await users.CheckPasswordAsync(new AppUser(), req.Password);   // roughly equal work whether or not the account exists
            return invalid;
        }

        if (opt.Value.RequireConfirmedEmail && !user.EmailConfirmed && await users.CheckPasswordAsync(user, req.Password))
        {
            return Results.Json(new ApiError(ErrorCodes.EmailNotConfirmed, "請先到信箱點擊確認連結。", ctx.TraceIdentifier), statusCode: 403);
        }

        var res = await signIn.PasswordSignInAsync(user, req.Password, isPersistent: true, lockoutOnFailure: true);
        if (res.IsLockedOut)
        {
            return Results.Json(new ApiError(ErrorCodes.RateLimited, "嘗試次數太多，請 15 分鐘後再試。", ctx.TraceIdentifier), statusCode: 429);
        }
        return res.Succeeded ? Results.Ok(ToDto(user)) : invalid;
    }

    private static async Task<IResult> Logout(SignInManager<AppUser> signIn)
    {
        await signIn.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? Results.Unauthorized() : Results.Ok(ToDto(user));
    }

    private static async Task<IResult> ConfirmEmail(ConfirmEmailRequest req, UserManager<AppUser> users, HttpContext ctx)
    {
        var user = await users.FindByIdAsync(req.UserId.ToString());
        var token = Decode(req.Token);
        if (user is null || token is null || !(await users.ConfirmEmailAsync(user, token)).Succeeded)
        {
            return Results.Json(new ApiError(ErrorCodes.InvalidToken, "確認連結無效或已過期。", ctx.TraceIdentifier), statusCode: 422);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> ResendConfirmation(EmailRequest req, UserManager<AppUser> users, IMailSender mail,
        IOptions<AuthOptions> opt, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email.Trim());
        if (user is { EmailConfirmed: false })
        {
            await SendConfirmation(user, users, mail, opt, ct);
        }
        return Results.Accepted();           // same answer whether or not the account exists
    }

    private static async Task<IResult> ForgotPassword(EmailRequest req, UserManager<AppUser> users, IMailSender mail,
        IOptions<AuthOptions> opt, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email.Trim());
        if (user is not null)
        {
            var token = Encode(await users.GeneratePasswordResetTokenAsync(user));
            var (subject, html, text) = MailTemplates.ResetPassword(user.DisplayName,
                Link(opt.Value, "/reset-password", ("email", user.Email!), ("token", token)));
            await mail.SendAsync(user.Email!, subject, html, text, ct);
        }
        return Results.Accepted();           // never reveals whether the e-mail is registered
    }

    private static async Task<IResult> ResetPassword(ResetPasswordRequest req, UserManager<AppUser> users, HttpContext ctx)
    {
        var user = await users.FindByEmailAsync(req.Email.Trim());
        var token = Decode(req.Token);
        if (user is null || token is null)
        {
            return Results.Json(new ApiError(ErrorCodes.InvalidToken, "重設連結無效或已過期。", ctx.TraceIdentifier), statusCode: 422);
        }

        var result = await users.ResetPasswordAsync(user, token, req.NewPassword);
        if (!result.Succeeded)
        {
            return Results.Json(new ApiError(ErrorCodes.InvalidToken, "重設連結無效或已過期。", ctx.TraceIdentifier), statusCode: 422);
        }

        await users.UpdateSecurityStampAsync(user);      // sign out every existing session
        return Results.NoContent();
    }
}
