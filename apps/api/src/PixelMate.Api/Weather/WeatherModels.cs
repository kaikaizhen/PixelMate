using FluentValidation;

namespace PixelMate.Api.Weather;

/// <summary>The weather words the pet world understands. The picture for the time of day (sunrise, day, sunset, night) is chosen by each device from its own local clock.</summary>
public static class Conditions
{
    public const string Clear = "clear", Cloudy = "cloudy", Fog = "fog", Drizzle = "drizzle", Shower = "shower", Thunder = "thunder",
        Typhoon = "typhoon", Snow = "snow", Cold = "cold", Petals = "petals";
    public static readonly string[] All = [Clear, Cloudy, Fog, Drizzle, Shower, Thunder, Typhoon, Snow, Cold, Petals];
}

public static class Sources
{
    public const string Cwa = "cwa", OpenMeteo = "open-meteo", Simulated = "simulated";
}

public sealed record WeatherSnapshot(string Condition, double? TemperatureC, string Source, DateTime FetchedAt);

public sealed class WeatherOptions
{
    /// <summary>Central Weather Administration open-data authorization (CWA-...). Empty: CWA is skipped and Open-Meteo is used everywhere.</summary>
    public string CwaApiKey { get; set; } = "";
    /// <summary>How long a fetched weather stays valid (6 hours). The first request after that fetches again.</summary>
    public int CacheSeconds { get; set; } = 6 * 3600;
    /// <summary>How long this server remembers its own copy before asking Redis again (so a refresh made by another instance shows up).</summary>
    public int LocalCacheSeconds { get; set; } = 600;
    public int TimeoutSeconds { get; set; } = 6;
    /// <summary>After a failed fetch, do not try the same source again for this long.</summary>
    public int RetryAfterFailureSeconds { get; set; } = 120;
    public string OpenMeteoBaseUrl { get; set; } = "https://api.open-meteo.com";
    public string CwaBaseUrl { get; set; } = "https://opendata.cwa.gov.tw";
    public int DefaultUtcOffsetHours { get; set; } = 8;
}

/// <summary><c>user_locations</c>: where a player lives, rounded to a 0.1 degree grid (about 10 km) so no exact position is kept.</summary>
public class UserLocation
{
    public Guid UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TimeZone { get; set; } = "Asia/Taipei";
    public string Label { get; set; } = "";
    public string Source { get; set; } = "city";          // gps | city
    public DateTime UpdatedAt { get; set; }
}

public sealed record SaveLocationRequest(double Latitude, double Longitude, string TimeZone, string? Label, string Source);
public sealed record LocationDto(double Latitude, double Longitude, string TimeZone, string Label, string Source, DateTime UpdatedAt);
public sealed record CityDto(string Id, string Name, double Latitude, double Longitude, string TimeZone, string Group);

public sealed class SaveLocationValidator : AbstractValidator<SaveLocationRequest>
{
    public SaveLocationValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("緯度不正確。");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithMessage("經度不正確。");
        RuleFor(x => x.TimeZone).NotEmpty().Must(WeatherRules.ValidTimeZone).WithMessage("時區不正確。");
        RuleFor(x => x.Label).MaximumLength(40);
        RuleFor(x => x.Source).Must(s => s is "gps" or "city").WithMessage("位置來源不正確。");
    }
}

public static class Cities
{
    public static readonly CityDto[] All =
    [
        new("tpe", "臺北市", 25.04, 121.56, "Asia/Taipei", "taiwan"), new("ntpc", "新北市", 25.01, 121.46, "Asia/Taipei", "taiwan"),
        new("kee", "基隆市", 25.13, 121.74, "Asia/Taipei", "taiwan"), new("tao", "桃園市", 24.99, 121.30, "Asia/Taipei", "taiwan"),
        new("hsz", "新竹市", 24.81, 120.97, "Asia/Taipei", "taiwan"), new("hsq", "新竹縣", 24.84, 121.01, "Asia/Taipei", "taiwan"),
        new("mia", "苗栗縣", 24.56, 120.82, "Asia/Taipei", "taiwan"), new("txg", "臺中市", 24.15, 120.67, "Asia/Taipei", "taiwan"),
        new("cha", "彰化縣", 24.07, 120.54, "Asia/Taipei", "taiwan"), new("nan", "南投縣", 23.96, 120.97, "Asia/Taipei", "taiwan"),
        new("yun", "雲林縣", 23.71, 120.43, "Asia/Taipei", "taiwan"), new("cyi", "嘉義市", 23.48, 120.45, "Asia/Taipei", "taiwan"),
        new("cyq", "嘉義縣", 23.46, 120.26, "Asia/Taipei", "taiwan"), new("tnn", "臺南市", 22.99, 120.21, "Asia/Taipei", "taiwan"),
        new("khh", "高雄市", 22.63, 120.30, "Asia/Taipei", "taiwan"), new("pif", "屏東縣", 22.67, 120.49, "Asia/Taipei", "taiwan"),
        new("ila", "宜蘭縣", 24.75, 121.75, "Asia/Taipei", "taiwan"), new("hua", "花蓮縣", 23.99, 121.60, "Asia/Taipei", "taiwan"),
        new("ttt", "臺東縣", 22.76, 121.14, "Asia/Taipei", "taiwan"), new("peh", "澎湖縣", 23.57, 119.58, "Asia/Taipei", "taiwan"),
        new("kmn", "金門縣", 24.45, 118.38, "Asia/Taipei", "taiwan"), new("lie", "連江縣", 26.16, 119.95, "Asia/Taipei", "taiwan"),
        new("tyo", "東京", 35.68, 139.69, "Asia/Tokyo", "world"), new("osa", "大阪", 34.69, 135.50, "Asia/Tokyo", "world"),
        new("sel", "首爾", 37.57, 126.98, "Asia/Seoul", "world"), new("hkg", "香港", 22.32, 114.17, "Asia/Hong_Kong", "world"),
        new("sin", "新加坡", 1.35, 103.82, "Asia/Singapore", "world"), new("bkk", "曼谷", 13.76, 100.50, "Asia/Bangkok", "world"),
        new("lon", "倫敦", 51.51, -0.13, "Europe/London", "world"), new("par", "巴黎", 48.86, 2.35, "Europe/Paris", "world"),
        new("nyc", "紐約", 40.71, -74.01, "America/New_York", "world"), new("lax", "洛杉磯", 34.05, -118.24, "America/Los_Angeles", "world"),
        new("syd", "雪梨", -33.87, 151.21, "Australia/Sydney", "world"),
    ];
}
