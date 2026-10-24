using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;

namespace PixelMate.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealth(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/health");

        // Liveness: no dependencies, no internals.
        g.MapGet("/", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

        // Internal diagnostic: only when Health:ExposeDb is on (dev / private network). Never returns driver messages.
        g.MapGet("/db", async (AppDbContext db, IConfiguration cfg, HttpContext ctx, CancellationToken ct) =>
        {
            if (!cfg.GetValue<bool>("Health:ExposeDb"))
            {
                return Results.NotFound();
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            bool ok;
            try
            {
                ok = await db.Database.CanConnectAsync(timeout.Token);
            }
            catch (Exception)
            {
                ok = false;
            }

            return ok
                ? Results.Ok(new { status = "ok", database = "reachable" })
                : Results.Json(new ApiError(ErrorCodes.ServiceUnavailable, "資料庫目前無法連線。", ctx.TraceIdentifier),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });
        return api;
    }
}
