namespace PixelMate.Api.Life;

public static class Scenes
{
    public const string Home = "home", Balcony = "balcony", Yard = "yard";
    public static readonly string[] All = [Home, Balcony, Yard];
}

/// <summary>
/// Where the pet is and what it looks like outside, as pure functions (same answer on every device). The weather is a simulated
/// "pet world" weather that is the same for everyone until a real weather source is connected (B9).
/// </summary>
public static class WorldRules
{
    /// <summary>The window/balcony/yard art that exists: sunrise, sunset, starry (times of day) and nine kinds of weather.</summary>
    public static readonly string[] WeatherKeys =
        ["sunrise", "sunset", "starry", "cloudy", "fog", "drizzle", "shower", "thunder", "typhoon", "petals", "cold", "snow"];

    /// <summary>The scene of a slot: any activity may happen anywhere, but walks favour the outdoors and idling favours home.</summary>
    public static string SceneAt(Guid petId, string activity, long slot)
    {
        var (home, balcony) = activity switch
        {
            Activities.Walk => (0.15, 0.40),            // cumulative: home 15%, balcony 25%, yard 60%
            Activities.Idle => (0.70, 0.85),
            _ => (0.50, 0.75),
        };
        var r = LifeScheduler.Roll(petId, slot, "scene");
        return r < home ? Scenes.Home : r < balcony ? Scenes.Balcony : Scenes.Yard;
    }

    /// <summary>The simulated weather condition (no time of day): used when no real weather can be fetched.</summary>
    public static string ConditionAt(DateTime utc, int utcOffsetHours)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(utcOffsetHours);
        return Pick(local);
    }

    private static string Pick(DateTime local)
    {
        var block = (long)Math.Floor((local.Date - DateTime.UnixEpoch.Date).TotalDays) * 4 + local.Hour / 6;
        var m = local.Month;
        (string Key, int Weight)[] conditions =
        [
            ("clear", 40), ("cloudy", 20), ("fog", 8), ("drizzle", 8), ("shower", 8), ("thunder", 4), ("typhoon", 2),
            ("petals", m is >= 3 and <= 5 ? 8 : 0), ("cold", m is 11 or 12 or 1 or 2 ? 6 : 0), ("snow", m is 12 or 1 or 2 ? 4 : 0),
        ];
        var pick = LifeScheduler.Roll(Guid.Empty, block, "weather") * conditions.Sum(c => c.Weight);
        foreach (var (key, weight) in conditions)
        {
            if ((pick -= weight) < 0) return key;
        }
        return "clear";
    }

    /// <summary>Weather for a moment: one condition per 6 local hours (shared by everyone), shown as the time of day when it is clear.</summary>
    public static string WeatherAt(DateTime utc, int utcOffsetHours)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(utcOffsetHours);
        var chosen = Pick(local);
        if (chosen != "clear") return chosen;
        return local.Hour switch { >= 5 and < 9 => "sunrise", >= 9 and < 17 => "cloudy", >= 17 and < 20 => "sunset", _ => "starry" };
    }
}
