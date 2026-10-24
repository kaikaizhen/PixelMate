using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PixelMate.Api.Infrastructure;

namespace PixelMate.Api.Tests;

public class HealthTests
{
    [Fact]
    public async Task Health_returns_ok_and_a_request_id()
    {
        using var f = new ApiFactory();
        var res = await f.CreateClient().GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(res.Headers.TryGetValues("X-Request-Id", out var ids) && ids.Single().StartsWith("req-"));
        using var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unknown_route_uses_the_error_envelope()
    {
        using var f = new ApiFactory();
        var res = await f.CreateClient().GetAsync("/api/v1/nope");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var err = await res.Content.ReadFromJsonAsync<ApiError>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(ErrorCodes.NotFound, err!.Code);
        Assert.False(string.IsNullOrEmpty(err.RequestId));
    }

    [Fact]
    public async Task Db_diagnostic_is_hidden_by_default()
    {
        using var f = new ApiFactory();
        var res = await f.CreateClient().GetAsync("/api/v1/health/db");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Db_diagnostic_reports_503_without_leaking_driver_details()
    {
        using var f = new ApiFactory(new Dictionary<string, string?> { ["Health:ExposeDb"] = "true" });
        var res = await f.CreateClient().GetAsync("/api/v1/health/db");
        var text = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        Assert.Contains(ErrorCodes.ServiceUnavailable, text);
        Assert.DoesNotContain("127.0.0.1", text);
        Assert.DoesNotContain("Pwd", text);
    }

    [Fact]
    public async Task Rate_limit_returns_429_with_the_error_envelope()
    {
        using var f = new ApiFactory(new Dictionary<string, string?> { ["RateLimit:PermitPerMinute"] = "2" });
        var c = f.CreateClient();
        await c.GetAsync("/api/v1/health");
        await c.GetAsync("/api/v1/health");
        var res = await c.GetAsync("/api/v1/health");

        Assert.Equal((HttpStatusCode)429, res.StatusCode);
        var err = await res.Content.ReadFromJsonAsync<ApiError>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(ErrorCodes.RateLimited, err!.Code);
    }

    [Fact]
    public async Task Exceptions_map_to_the_envelope_and_never_leak_details()
    {
        var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext { TraceIdentifier = "req-test" };
        ctx.Response.Body = new MemoryStream();

        await ErrorHandling.HandleAsync(ctx, new InvalidOperationException("secret connection string"),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        ctx.Response.Body.Position = 0;
        var text = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        Assert.Equal(500, ctx.Response.StatusCode);
        Assert.Contains(ErrorCodes.Internal, text);
        Assert.DoesNotContain("secret", text);
    }

    [Fact]
    public async Task Api_exceptions_keep_their_status_and_code()
    {
        var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext { TraceIdentifier = "req-test" };
        ctx.Response.Body = new MemoryStream();

        await ErrorHandling.HandleAsync(ctx, new ApiException(409, ErrorCodes.PetAlreadyExists, "已有寵物。"),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        Assert.Equal(409, ctx.Response.StatusCode);
    }
}
