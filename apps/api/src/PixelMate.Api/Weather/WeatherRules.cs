using System.Globalization;

namespace PixelMate.Api.Weather;

/// <summary>Turning what the weather services say into the pet world's words. Pure functions, covered by tests.</summary>
public static class WeatherRules
{
    public static bool ValidTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64) return false;
        try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; } catch (Exception) { return false; }
    }

    /// <summary>The grid cell a position belongs to (0.1 degree, about 10 km): requests and cache entries are shared inside it.</summary>
    public static (double Lat, double Lon) Cell(double lat, double lon) =>
        (Math.Round(lat, 1, MidpointRounding.AwayFromZero), Math.Round(lon, 1, MidpointRounding.AwayFromZero));

    public static bool IsTaiwan(double lat, double lon) => lat is >= 21.5 and <= 26.5 && lon is >= 118.0 and <= 122.5;

    /// <summary>WMO weather interpretation codes (Open-Meteo) to a condition. Strong wind with rain is typhoon weather.</summary>
    public static string FromWmo(int code, double? windKmh, double? precipMm)
    {
        var c = code switch
        {
            0 or 1 => Conditions.Clear,
            2 or 3 => Conditions.Cloudy,
            45 or 48 => Conditions.Fog,
            51 or 53 or 55 or 56 or 57 or 61 => Conditions.Drizzle,
            63 or 65 or 66 or 67 or 80 or 81 or 82 => Conditions.Shower,
            71 or 73 or 75 or 77 or 85 or 86 => Conditions.Snow,
            95 or 96 or 99 => Conditions.Thunder,
            _ => Conditions.Cloudy,
        };
        var wet = c is Conditions.Drizzle or Conditions.Shower or Conditions.Thunder || precipMm > 0;
        return windKmh >= 62 && wet ? Conditions.Typhoon : c;
    }

    /// <summary>The Chinese weather phrases of the Central Weather Administration ("晴", "多雲", "陰有陣雨", "晴有靄"...). Null when it says nothing usable.</summary>
    public static string? FromCwaText(string? text, double? precipMm)
    {
        var t = (text ?? "").Trim();
        if (t.Length > 0 && !t.StartsWith('-'))
        {
            if (t.Contains('颱')) return Conditions.Typhoon;
            if (t.Contains('雷')) return Conditions.Thunder;
            if (t.Contains('雪')) return Conditions.Snow;
            if (t.Contains('霧') || t.Contains('靄')) return Conditions.Fog;
            if (t.Contains("毛毛雨") || t.Contains("小雨")) return Conditions.Drizzle;
            if (t.Contains('雨')) return Conditions.Shower;
            if (t.Contains('陰') || t.Contains('雲')) return Conditions.Cloudy;
            if (t.Contains('晴')) return Conditions.Clear;
        }
        return precipMm switch { >= 7.6 => Conditions.Shower, > 0 => Conditions.Drizzle, _ => null };
    }

    /// <summary>Cold air makes clear or cloudy weather "cold"; mild spring days sometimes carry petals (decoration, chosen per day and place).</summary>
    public static string Decorate(string condition, double? tempC, DateTime localDate, double lat, double lon)
    {
        if (condition is Conditions.Clear or Conditions.Cloudy)
        {
            if (tempC <= 12) return Conditions.Cold;
            if (condition == Conditions.Clear && localDate.Month is 3 or 4 && tempC is >= 15 and <= 27)
            {
                var mix = ((long)localDate.DayOfYear * 73856093L) ^ ((long)Math.Round(lat * 10) * 19349663L) ^ ((long)Math.Round(lon * 10) * 83492791L);
                if ((mix & 0x7fffffff) % 3 == 0) return Conditions.Petals;
            }
        }
        return condition;
    }

    public static double? Number(string? s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > -90 ? v : null;   // CWA writes -99 / -998 for "no data"

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double Rad(double d) => d * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lon2 - lon1) / 2), 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
}
