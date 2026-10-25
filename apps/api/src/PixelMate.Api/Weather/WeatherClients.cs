using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PixelMate.Api.Weather;

public sealed record CwaStation(string Name, double Lat, double Lon, string? WeatherText, double? TempC, double? PrecipMm);
public sealed record RawWeather(string Condition, double? TempC);

/// <summary>Open-Meteo (free, no key): the current weather at a position anywhere in the world.</summary>
public sealed class OpenMeteoClient(IHttpClientFactory http, IOptions<WeatherOptions> options)
{
    public async Task<RawWeather> CurrentAsync(double lat, double lon, CancellationToken ct)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"/v1/forecast?latitude={lat:0.0}&longitude={lon:0.0}&current=temperature_2m,weather_code,wind_speed_10m,precipitation&wind_speed_unit=kmh&timezone=auto");
        using var res = await http.CreateClient("openmeteo").GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var cur = doc.RootElement.GetProperty("current");
        double? Get(string n) => cur.TryGetProperty(n, out var e) && e.ValueKind == JsonValueKind.Number ? e.GetDouble() : null;
        var code = (int)(Get("weather_code") ?? throw new InvalidDataException("Open-Meteo sent no weather_code"));
        return new RawWeather(WeatherRules.FromWmo(code, Get("wind_speed_10m"), Get("precipitation")), Get("temperature_2m"));
    }
}

/// <summary>Central Weather Administration open data, dataset O-A0003-001 (current observations of every station). One download serves every place in Taiwan.</summary>
public sealed class CwaClient(IHttpClientFactory http, IOptions<WeatherOptions> options)
{
    public bool Enabled => !string.IsNullOrWhiteSpace(options.Value.CwaApiKey);

    public async Task<List<CwaStation>> ObservationsAsync(CancellationToken ct)
    {
        var url = $"/api/v1/rest/datastore/O-A0003-001?Authorization={Uri.EscapeDataString(options.Value.CwaApiKey)}&format=JSON";
        using var res = await http.CreateClient("cwa").GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return Parse(doc.RootElement);
    }

    /// <summary>Reads both layouts the service has used: records.Station[] (current) and records.location[] (older).</summary>
    public static List<CwaStation> Parse(JsonElement root)
    {
        var list = new List<CwaStation>();
        if (!root.TryGetProperty("records", out var records)) return list;
        if (records.TryGetProperty("Station", out var stations) && stations.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in stations.EnumerateArray())
            {
                if (!s.TryGetProperty("GeoInfo", out var geo) || !geo.TryGetProperty("Coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array) continue;
                JsonElement? wgs = null;
                foreach (var c in coords.EnumerateArray()) if (Str(c, "CoordinateName") == "WGS84") wgs = c;
                wgs ??= coords.EnumerateArray().FirstOrDefault();
                var lat = WeatherRules.Number(Str(wgs, "StationLatitude")); var lon = WeatherRules.Number(Str(wgs, "StationLongitude"));
                if (lat is null || lon is null) continue;
                var el = s.TryGetProperty("WeatherElement", out var we) ? we : default;
                var now = el.ValueKind == JsonValueKind.Object && el.TryGetProperty("Now", out var n) ? n : default;
                list.Add(new CwaStation(Str(s, "StationName") ?? "", lat.Value, lon.Value, Str(el, "Weather"), WeatherRules.Number(Str(el, "AirTemperature")), WeatherRules.Number(Str(now, "Precipitation"))));
            }
        }
        else if (records.TryGetProperty("location", out var locations) && locations.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in locations.EnumerateArray())
            {
                var lat = WeatherRules.Number(Str(l, "lat")); var lon = WeatherRules.Number(Str(l, "lon"));
                if (lat is null || lon is null) continue;
                string? text = null; double? temp = null, precip = null;
                if (l.TryGetProperty("weatherElement", out var els) && els.ValueKind == JsonValueKind.Array)
                    foreach (var e in els.EnumerateArray())
                    {
                        var name = Str(e, "elementName"); var value = Str(e, "elementValue");
                        if (name == "Weather") text = value; else if (name == "TEMP") temp = WeatherRules.Number(value); else if (name == "HOUR_24") precip = WeatherRules.Number(value);
                    }
                list.Add(new CwaStation(Str(l, "locationName") ?? "", lat.Value, lon.Value, text, temp, precip));
            }
        }
        return list;
    }

    /// <summary>The nearest station (within 30 km) that reports something usable.</summary>
    public static RawWeather? Nearest(IReadOnlyList<CwaStation> stations, double lat, double lon)
    {
        foreach (var s in stations.Select(s => (s, d: WeatherRules.DistanceKm(lat, lon, s.Lat, s.Lon))).Where(x => x.d <= 30).OrderBy(x => x.d).Select(x => x.s))
        {
            var cond = WeatherRules.FromCwaText(s.WeatherText, s.PrecipMm);
            if (cond is not null) return new RawWeather(cond, s.TempC);
        }
        return null;
    }

    private static string? Str(JsonElement? e, string name)
    {
        if (e is not { ValueKind: JsonValueKind.Object } o || !o.TryGetProperty(name, out var p)) return null;
        return p.ValueKind switch { JsonValueKind.String => p.GetString(), JsonValueKind.Number => p.GetRawText(), _ => null };
    }
}
