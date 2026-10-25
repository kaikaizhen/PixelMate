using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using PixelMate.Api.Life;

namespace PixelMate.Api.Weather;

/// <summary>
/// The weather for a place, from the Central Weather Administration in Taiwan and Open-Meteo elsewhere (or when CWA fails / has no key).
/// Answers are shared per 0.1 degree cell through HybridCache (in memory + Redis) and live 6 hours, so the services are asked a few times a day.
/// If both are down, the pet world's own simulated weather is used (and the services are left alone for a couple of minutes).
/// </summary>
public sealed class WeatherService(HybridCache cache, OpenMeteoClient openMeteo, CwaClient cwa, IOptions<WeatherOptions> options, TimeProvider clock, ILogger<WeatherService> log)
{
    private readonly WeatherOptions _o = options.Value;
    private readonly ConcurrentDictionary<string, DateTime> _failedUntil = new();

    private HybridCacheEntryOptions Entry => new()
    {
        Expiration = TimeSpan.FromSeconds(_o.CacheSeconds),
        LocalCacheExpiration = TimeSpan.FromSeconds(Math.Min(_o.LocalCacheSeconds, _o.CacheSeconds)),
    };

    private bool Cooling(string what) => _failedUntil.TryGetValue(what, out var until) && until > clock.GetUtcNow().UtcDateTime;
    private void Fail(string what, Exception e)
    {
        _failedUntil[what] = clock.GetUtcNow().UtcDateTime.AddSeconds(_o.RetryAfterFailureSeconds);
        log.LogWarning("Weather source {Source} failed ({Type}); not asking it again for {Seconds}s.", what, e.GetType().Name, _o.RetryAfterFailureSeconds);
    }

    public async Task<WeatherSnapshot> GetAsync(double lat, double lon, int utcOffsetHours, CancellationToken ct)
    {
        var (clat, clon) = WeatherRules.Cell(lat, lon);
        if (Cooling("all")) return Simulated(utcOffsetHours);
        try
        {
            return await cache.GetOrCreateAsync($"weather:v1:{clat:0.0}:{clon:0.0}", async token => await FetchAsync(clat, clon, token), Entry, cancellationToken: ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Fail("all", e);
            return Simulated(utcOffsetHours);
        }
    }

    private async Task<WeatherSnapshot> FetchAsync(double lat, double lon, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var local = now.AddHours(_o.DefaultUtcOffsetHours);
        if (WeatherRules.IsTaiwan(lat, lon) && cwa.Enabled && !Cooling("cwa"))
        {
            try
            {
                var stations = await cache.GetOrCreateAsync("weather:cwa:v1", async token => await cwa.ObservationsAsync(token), Entry, cancellationToken: ct);
                if (CwaClient.Nearest(stations, lat, lon) is { } raw)
                    return new WeatherSnapshot(WeatherRules.Decorate(raw.Condition, raw.TempC, local, lat, lon), raw.TempC, Sources.Cwa, now);
            }
            catch (Exception e) when (e is not OperationCanceledException) { Fail("cwa", e); }
        }
        if (Cooling("open-meteo")) throw new InvalidOperationException("Open-Meteo is cooling down");
        try
        {
            var raw = await openMeteo.CurrentAsync(lat, lon, ct);
            return new WeatherSnapshot(WeatherRules.Decorate(raw.Condition, raw.TempC, local, lat, lon), raw.TempC, Sources.OpenMeteo, now);
        }
        catch (Exception e) when (e is not OperationCanceledException) { Fail("open-meteo", e); throw; }
    }

    private WeatherSnapshot Simulated(int utcOffsetHours)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new WeatherSnapshot(WorldRules.ConditionAt(now, utcOffsetHours), null, Sources.Simulated, now);
    }
}
