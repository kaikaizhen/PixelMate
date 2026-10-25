using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PixelMate.Api.Infrastructure;

namespace PixelMate.Api.Tests;

public class AuthTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<ApiError> Err(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ApiError>(Web))!;

    [Fact]
    public async Task Register_login_me_logout_round_trip()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await AuthFactory.Register(c)).StatusCode);

        var login = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "A@Example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(x => x.Contains("httponly", StringComparison.OrdinalIgnoreCase) && x.Contains(".pixelmate.auth")));

        var me = await c.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal("小栗", me.GetProperty("displayName").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Duplicate_email_is_409()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        await AuthFactory.Register(c);
        var again = await AuthFactory.Register(c, "A@EXAMPLE.com");
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(ErrorCodes.EmailTaken, (await Err(again)).Code);
    }

    [Theory]
    [InlineData("short1", "password")]
    [InlineData("onlyletterspassword", "password")]
    [InlineData("1234567890123", "password")]
    public async Task Weak_passwords_are_422_with_field_errors(string password, string field)
    {
        using var f = new AuthFactory();
        var res = await AuthFactory.Register(f.NewClient(), password: password);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        var err = await Err(res);
        Assert.Equal(ErrorCodes.ValidationFailed, err.Code);
        Assert.True(err.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Bad_email_and_html_names_are_rejected()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        Assert.Equal((HttpStatusCode)422, (await AuthFactory.Register(c, email: "not-an-email")).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await AuthFactory.Register(c, name: "<script>")).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_account_give_the_same_answer()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        await AuthFactory.Register(c);

        var wrong = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Wrong-pass-1" });
        var unknown = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "nobody@example.com", password = "Wrong-pass-1" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal((await Err(wrong)).Message, (await Err(unknown)).Message);
    }

    [Fact]
    public async Task Five_failures_lock_the_account_even_for_the_right_password()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        await AuthFactory.Register(c);
        for (var i = 0; i < 5; i++)
        {
            await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Wrong-pass-1" });
        }
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        Assert.Equal((HttpStatusCode)429, res.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_never_reveals_whether_the_email_exists()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        await AuthFactory.Register(c);
        f.Mail.Sent.Clear();

        var unknown = await c.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ghost@example.com" });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Empty(f.Mail.Sent);

        var known = await c.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "a@example.com" });
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Single(f.Mail.Sent);
    }

    [Fact]
    public async Task Reset_password_works_once_and_signs_out_other_sessions()
    {
        using var f = new AuthFactory();
        var other = f.NewClient();                      // a second device that is already signed in
        await AuthFactory.Register(other);
        await other.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/v1/auth/me")).StatusCode);

        var c = f.NewClient();
        f.Mail.Sent.Clear();
        await c.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "a@example.com" });
        var mail = f.Mail.Sent.Single();
        Assert.Contains("/reset-password", mail.Link);
        var token = mail.Query("token");

        var reset = await c.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = "a@example.com", token, newPassword = "Brand-new-pass9" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Brand-new-pass9" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/v1/auth/me")).StatusCode);   // old session is dead

        var reuse = await c.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = "a@example.com", token, newPassword = "Another-pass-7" });
        Assert.Equal((HttpStatusCode)422, reuse.StatusCode);
        Assert.Equal(ErrorCodes.InvalidToken, (await Err(reuse)).Code);
    }

    [Fact]
    public async Task Garbage_reset_tokens_are_rejected()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        await AuthFactory.Register(c);
        var res = await c.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = "a@example.com", token = "%%%not-a-token", newPassword = "Brand-new-pass9" });
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
    }

    [Fact]
    public async Task Email_confirmation_is_enforced_when_required()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Auth:RequireConfirmedEmail"] = "true" });
        var c = f.NewClient();
        await AuthFactory.Register(c);
        var mail = f.Mail.Sent.Single();

        var blocked = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal(ErrorCodes.EmailNotConfirmed, (await Err(blocked)).Code);

        var confirm = await c.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = mail.Query("userId"), token = mail.Query("token") });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" })).StatusCode);
    }

    [Fact]
    public async Task State_changing_requests_without_the_csrf_header_are_rejected()
    {
        using var f = new AuthFactory();
        f.NewClient();                                   // creates the schema
        var raw = f.CreateClient();                      // no X-Requested-With
        var res = await raw.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(ErrorCodes.CsrfRejected, (await Err(res)).Code);
    }

    [Fact]
    public async Task Auth_endpoints_have_their_own_stricter_rate_limit()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["RateLimit:AuthPerMinute"] = "3" });
        var c = f.NewClient();
        for (var i = 0; i < 3; i++)
        {
            await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "x" });
        }
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "x" });
        Assert.Equal((HttpStatusCode)429, res.StatusCode);
        Assert.Equal(ErrorCodes.RateLimited, (await Err(res)).Code);
    }
}
