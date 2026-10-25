using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Weather;

namespace PixelMate.Api.Tests;

public class WeatherTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<HttpClient> Player(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        await c.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" });
        return c;
    }

    private static async Task<JsonElement> World(HttpClient c) => (await c.GetFromJsonAsync<JsonElement>("/api/v1/pets/me", Web)).GetProperty("world");

    private static Task Move(HttpClient c, double lat, double lon, string tz = "Asia/Taipei", string source = "city") =>
        c.PutAsJsonAsync("/api/v1/me/location", new { latitude = lat, longitude = lon, timeZone = tz, label = "x", source });

    private const string CwaTaipei = """{"records":{"Station":[{"StationName":"臺北","GeoInfo":{"Coordinates":[{"CoordinateName":"TWD67","StationLatitude":"25.0","StationLongitude":"121.5"},{"CoordinateName":"WGS84","StationLatitude":"25.04","StationLongitude":"121.51"}]},"WeatherElement":{"Weather":"陰有陣雨","AirTemperature":"21.4","WindSpeed":"2.0","Now":{"Precipitation":"1.5"}}},{"StationName":"高雄","GeoInfo":{"Coordinates":[{"CoordinateName":"WGS84","StationLatitude":"22.63","StationLongitude":"120.30"}]},"WeatherElement":{"Weather":"晴","AirTemperature":"29.0","Now":{"Precipitation":"0.0"}}}]}}""";

    // ---- the translation rules ---------------------------------------------------------------------------------------
    [Theory]
    [InlineData(0, null, null, "clear")]
    [InlineData(3, null, null, "cloudy")]
    [InlineData(45, null, null, "fog")]
    [InlineData(53, null, null, "drizzle")]
    [InlineData(65, null, null, "shower")]
    [InlineData(81, null, null, "shower")]
    [InlineData(73, null, null, "snow")]
    [InlineData(95, null, null, "thunder")]
    [InlineData(65, 70.0, 4.0, "typhoon")]            // strong wind with rain
    [InlineData(0, 70.0, 0.0, "clear")]               // wind alone is not a typhoon
    public void Open_meteo_codes_become_pet_world_weather(int code, double? wind, double? rain, string expected) =>
        Assert.Equal(expected, WeatherRules.FromWmo(code, wind, rain));

    [Theory]
    [InlineData("晴", null, "clear")]
    [InlineData("多雲", null, "cloudy")]
    [InlineData("陰", null, "cloudy")]
    [InlineData("晴有靄", null, "fog")]
    [InlineData("有霧", null, "fog")]
    [InlineData("陰有陣雨", null, "shower")]
    [InlineData("小雨", null, "drizzle")]
    [InlineData("晴有雷雨", null, "thunder")]
    [InlineData("雪", null, "snow")]
    [InlineData("颱風", null, "typhoon")]
    [InlineData("-99", 0.5, "drizzle")]               // no phrase, but it is raining
    [InlineData("-99", 12.0, "shower")]
    public void Cwa_phrases_become_pet_world_weather(string text, double? rain, string expected) =>
        Assert.Equal(expected, WeatherRules.FromCwaText(text, rain));

    [Fact]
    public void A_missing_cwa_reading_says_nothing_instead_of_inventing_weather()
    {
        Assert.Null(WeatherRules.FromCwaText("-99", null));
        Assert.Null(WeatherRules.FromCwaText("", 0));
        Assert.Null(WeatherRules.Number("-99"));
        Assert.Equal(21.4, WeatherRules.Number("21.4"));
    }

    [Fact]
    public void Cold_clear_days_are_cold_and_mild_spring_days_may_carry_petals_but_rain_stays_rain()
    {
        var spring = new DateTime(2026, 3, 20);
        Assert.Equal("cold", WeatherRules.Decorate("clear", 9, spring, 25, 121.5));
        Assert.Equal("cold", WeatherRules.Decorate("cloudy", 12, spring, 25, 121.5));
        Assert.Equal("shower", WeatherRules.Decorate("shower", 9, spring, 25, 121.5));
        var petals = Enumerable.Range(0, 30).Select(d => WeatherRules.Decorate("clear", 20, spring.AddDays(d % 40), 25 + d * 0.1, 121.5)).Count(c => c == "petals");
        Assert.InRange(petals, 3, 20);
        Assert.Equal("clear", WeatherRules.Decorate("clear", 20, new DateTime(2026, 8, 1), 25, 121.5));    // never in summer
    }

    [Fact]
    public void Positions_share_a_cell_and_taiwan_is_recognised()
    {
        Assert.Equal(WeatherRules.Cell(25.0432, 121.5611), WeatherRules.Cell(25.0499, 121.5501));
        Assert.NotEqual(WeatherRules.Cell(25.04, 121.56), WeatherRules.Cell(25.16, 121.56));
        Assert.True(WeatherRules.IsTaiwan(24.45, 118.38));        // Kinmen
        Assert.False(WeatherRules.IsTaiwan(35.68, 139.69));       // Tokyo
    }

    [Fact]
    public void The_cwa_dataset_is_read_in_both_layouts_and_the_nearest_reporting_station_wins()
    {
        var stations = CwaClient.Parse(JsonDocument.Parse(CwaTaipei).RootElement);
        Assert.Equal(2, stations.Count);
        Assert.Equal(25.04, stations[0].Lat);                                   // the WGS84 coordinates, not TWD67
        Assert.Equal("shower", CwaClient.Nearest(stations, 25.05, 121.52)!.Condition);
        Assert.Equal("clear", CwaClient.Nearest(stations, 22.6, 120.3)!.Condition);
        Assert.Null(CwaClient.Nearest(stations, 24.0, 121.0));                  // no station within 30 km

        const string legacy = """{"records":{"location":[{"lat":"24.15","lon":"120.68","locationName":"臺中","weatherElement":[{"elementName":"Weather","elementValue":"多雲"},{"elementName":"TEMP","elementValue":"26.0"}]}]}}""";
        var old = CwaClient.Parse(JsonDocument.Parse(legacy).RootElement);
        Assert.Equal("cloudy", CwaClient.Nearest(old, 24.15, 120.67)!.Condition);
        Assert.Empty(CwaClient.Parse(JsonDocument.Parse("""{"success":"false"}""").RootElement));
    }

    // ---- through the API ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task Without_a_place_the_pet_lives_in_taipei_and_gets_open_meteo_weather()
    {
        using var f = new AuthFactory();
        var c = await Player(f);
        var w = await World(c);
        Assert.Equal("clear", w.GetProperty("weather").GetString());
        Assert.Equal("open-meteo", w.GetProperty("weatherSource").GetString());
        Assert.Equal(22.5, w.GetProperty("temperatureC").GetDouble());
        Assert.Equal(0, f.Weather.Count("cwa"));                                // no authorization key: CWA is not even asked
        Assert.Contains("latitude=25.0&longitude=121.5", f.Weather.Calls.Single());
    }

    [Fact]
    public async Task The_weather_is_fetched_once_per_cell_and_reused_by_everyone_there()
    {
        using var f = new AuthFactory();
        var a = await Player(f, "a@example.com");
        var b = await Player(f, "b@example.com");
        f.Weather.Calls.Clear();
        await Move(a, 25.041, 121.561); await Move(b, 25.046, 121.558);        // the same 0.1 degree cell
        for (var i = 0; i < 5; i++) { await World(a); await World(b); }
        Assert.Equal(1, f.Weather.Count("open-meteo"));

        await Move(b, 35.68, 139.69, "Asia/Tokyo");                             // another city is another request
        await World(b); await World(b);
        Assert.Equal(2, f.Weather.Count("open-meteo"));
    }

    [Fact]
    public async Task The_weather_expires_and_is_fetched_again()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Weather:CacheSeconds"] = "1", ["Weather:LocalCacheSeconds"] = "1" });
        var c = await Player(f);
        await World(c); await World(c);
        Assert.Equal(1, f.Weather.Count("open-meteo"));
        await Task.Delay(1500);
        await World(c);
        Assert.Equal(2, f.Weather.Count("open-meteo"));
    }

    [Fact]
    public async Task In_taiwan_with_a_key_the_cwa_answer_wins_and_the_dataset_is_downloaded_once()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Weather:CwaApiKey"] = "CWA-TEST" });
        f.Weather.CwaJson = CwaTaipei;
        var a = await Player(f, "a@example.com");
        var b = await Player(f, "b@example.com");
        await Move(a, 25.05, 121.52);
        await Move(b, 22.63, 120.30);
        var wa = await World(a); var wb = await World(b);
        Assert.Equal(("shower", "cwa"), (wa.GetProperty("weather").GetString(), wa.GetProperty("weatherSource").GetString()));
        Assert.Equal(("clear", "cwa"), (wb.GetProperty("weather").GetString(), wb.GetProperty("weatherSource").GetString()));
        Assert.Equal(21.4, wa.GetProperty("temperatureC").GetDouble());
        Assert.Equal(1, f.Weather.Count("cwa"));                                // one download served both cities
        Assert.Equal(0, f.Weather.Count("open-meteo"));
        Assert.Contains("Authorization=CWA-TEST", f.Weather.Calls.First());
    }

    [Fact]
    public async Task Abroad_only_open_meteo_is_asked()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Weather:CwaApiKey"] = "CWA-TEST" });
        var c = await Player(f);
        f.Weather.Calls.Clear();                                                // adopting the pet looked at the default place (Taipei)
        await Move(c, 35.68, 139.69, "Asia/Tokyo");
        var w = await World(c);
        Assert.Equal("open-meteo", w.GetProperty("weatherSource").GetString());
        Assert.Equal("Asia/Tokyo", w.GetProperty("timeZone").GetString());
        Assert.Equal(0, f.Weather.Count("cwa"));
    }

    [Fact]
    public async Task When_cwa_fails_open_meteo_takes_over_and_cwa_is_left_alone_for_a_while()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Weather:CwaApiKey"] = "CWA-TEST" });
        f.Weather.CwaDown = true;
        var a = await Player(f, "a@example.com");
        var b = await Player(f, "b@example.com");
        await Move(a, 25.05, 121.52); await Move(b, 24.15, 120.67);
        Assert.Equal("open-meteo", (await World(a)).GetProperty("weatherSource").GetString());
        Assert.Equal("open-meteo", (await World(b)).GetProperty("weatherSource").GetString());
        Assert.Equal(1, f.Weather.Count("cwa"));                                // the second city did not retry the broken source
    }

    [Fact]
    public async Task When_everything_is_down_the_pet_world_still_has_weather_and_the_services_are_not_hammered()
    {
        using var f = new AuthFactory();
        f.Weather.OpenMeteoDown = true;
        var c = await Player(f);
        for (var i = 0; i < 4; i++)
        {
            var w = await World(c);
            Assert.Equal("simulated", w.GetProperty("weatherSource").GetString());
            Assert.Contains(w.GetProperty("weather").GetString(), Conditions.All);
        }
        Assert.Equal(1, f.Weather.Count("open-meteo"));
    }

    [Fact]
    public async Task A_place_is_saved_rounded_validated_and_other_devices_are_told()
    {
        using var f = new AuthFactory();
        var c = await Player(f);
        Assert.Equal(JsonValueKind.Null, (await c.GetFromJsonAsync<JsonElement>("/api/v1/me/location", Web)).GetProperty("location").ValueKind);

        var r = await c.PutAsJsonAsync("/api/v1/me/location", new { latitude = 25.04321, longitude = 121.56149, timeZone = "Asia/Taipei", label = "  臺北市 ", source = "gps" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var loc = (await c.GetFromJsonAsync<JsonElement>("/api/v1/me/location", Web)).GetProperty("location");
        Assert.Equal(25.0, loc.GetProperty("latitude").GetDouble());                   // no exact position is kept
        Assert.Equal(121.6, loc.GetProperty("longitude").GetDouble());
        Assert.Equal("臺北市", loc.GetProperty("label").GetString());

        foreach (var bad in new object[]
        {
            new { latitude = 91.0, longitude = 0.0, timeZone = "Asia/Taipei", label = "x", source = "gps" },
            new { latitude = 0.0, longitude = 181.0, timeZone = "Asia/Taipei", label = "x", source = "gps" },
            new { latitude = 0.0, longitude = 0.0, timeZone = "Mars/Base", label = "x", source = "gps" },
            new { latitude = 0.0, longitude = 0.0, timeZone = "Asia/Taipei", label = "x", source = "magic" },
        })
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await c.PutAsJsonAsync("/api/v1/me/location", bad)).StatusCode);

        var events = (await c.GetFromJsonAsync<JsonElement>("/api/v1/events", Web)).GetProperty("events").EnumerateArray().Select(e => e.GetProperty("payload").GetProperty("reason").GetString());
        Assert.Contains("location", events);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/v1/me/location")).StatusCode);
    }

    [Fact]
    public async Task The_city_list_has_all_22_taiwan_counties_and_valid_time_zones()
    {
        using var f = new AuthFactory();
        var c = await Player(f);
        var cities = (await c.GetFromJsonAsync<JsonElement>("/api/v1/weather/cities", Web)).GetProperty("cities").EnumerateArray().ToList();
        Assert.Equal(22, cities.Count(x => x.GetProperty("group").GetString() == "taiwan"));
        Assert.All(cities, x => Assert.True(WeatherRules.ValidTimeZone(x.GetProperty("timeZone").GetString())));
        Assert.All(cities.Where(x => x.GetProperty("group").GetString() == "taiwan"), x => Assert.True(WeatherRules.IsTaiwan(x.GetProperty("latitude").GetDouble(), x.GetProperty("longitude").GetDouble())));
        Assert.Equal(cities.Count, cities.Select(x => x.GetProperty("id").GetString()).Distinct().Count());
    }
}
