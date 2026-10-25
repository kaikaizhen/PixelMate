using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Realtime;

namespace PixelMate.Api.Weather;

public static class WeatherEndpoints
{
    public static RouteGroupBuilder MapWeather(this RouteGroupBuilder api)
    {
        api.MapGet("/weather/cities", () => Results.Ok(new { cities = Cities.All })).RequireAuthorization();
        api.MapGet("/me/location", GetLocation).RequireAuthorization();
        api.MapPut("/me/location", SaveLocation).RequireAuthorization().Validate<SaveLocationRequest>();
        api.MapGet("/weather", Current).RequireAuthorization();
        return api;
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static LocationDto ToDto(UserLocation l) => new(l.Latitude, l.Longitude, l.TimeZone, l.Label, l.Source, l.UpdatedAt);

    private static async Task<IResult> GetLocation(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var me = UserId(principal);
        var l = await db.UserLocations.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == me, ct);
        return Results.Ok(new { location = l is null ? null : ToDto(l) });
    }

    /// <summary>Saves where the player is, rounded to the 0.1 degree grid (no exact position is kept). Other devices are told to re-read, since the weather follows the place.</summary>
    private static async Task<IResult> SaveLocation(SaveLocationRequest req, ClaimsPrincipal principal, AppDbContext db, EventOutbox outbox, TimeProvider clock, CancellationToken ct)
    {
        var me = UserId(principal);
        var (lat, lon) = WeatherRules.Cell(req.Latitude, req.Longitude);
        var l = await db.UserLocations.FirstOrDefaultAsync(x => x.UserId == me, ct);
        if (l is null) db.UserLocations.Add(l = new UserLocation { UserId = me });
        l.Latitude = lat; l.Longitude = lon; l.TimeZone = req.TimeZone; l.Source = req.Source;
        l.Label = (req.Label ?? "").Trim();
        l.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        outbox.Add(me, EventTypes.PetUpdated, new { reason = "location" });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();                                       // two devices saved the first location at once: the later one updates
            var again = await db.UserLocations.FirstAsync(x => x.UserId == me, ct);
            again.Latitude = lat; again.Longitude = lon; again.TimeZone = req.TimeZone; again.Source = req.Source; again.Label = (req.Label ?? "").Trim(); again.UpdatedAt = l.UpdatedAt;
            await db.SaveChangesAsync(ct);
            l = again;
        }
        return Results.Ok(ToDto(l));
    }

    private static async Task<IResult> Current(ClaimsPrincipal principal, AppDbContext db, WeatherService weather, CancellationToken ct)
    {
        var (w, loc) = await ForUser(UserId(principal), db, weather, ct);
        return Results.Ok(new { weather = w, location = loc });
    }

    /// <summary>The weather at the player's saved place (Taipei until they set one).</summary>
    internal static async Task<(WeatherSnapshot Weather, LocationDto? Location)> ForUser(Guid user, AppDbContext db, WeatherService weather, CancellationToken ct)
    {
        var l = await db.UserLocations.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user, ct);
        var offset = 8;
        if (l is not null) { try { offset = (int)Math.Round(TimeZoneInfo.FindSystemTimeZoneById(l.TimeZone).GetUtcOffset(DateTime.UtcNow).TotalHours); } catch (Exception) { /* keep 8 */ } }
        var w = await weather.GetAsync(l?.Latitude ?? 25.0, l?.Longitude ?? 121.5, offset, ct);
        return (w, l is null ? null : ToDto(l));
    }
}
