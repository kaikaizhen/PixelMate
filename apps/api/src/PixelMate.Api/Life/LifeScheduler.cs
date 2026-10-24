using System.Security.Cryptography;

namespace PixelMate.Api.Life;

public static class Activities
{
    public const string Idle = "idle", Walk = "walk", Read = "read", Hum = "hum", Letter = "letter";
    public static readonly string[] All = [Idle, Walk, Read, Hum, Letter];
}

public sealed record ActivityInfo(string Key, DateTime StartedAt, DateTime EndsAt);

/// <summary>
/// The shared life rules, as pure functions of (pet id, skill points, slot number): the API, the worker and the tests all call
/// these, so every device and every process gets the same answer without storing it. A slot is 15 minutes counted from the Unix epoch.
/// </summary>
public static class LifeScheduler
{
    public const int SlotSeconds = 15 * 60;
    public const int SlotsPerDay = 24 * 3600 / SlotSeconds;   // 96, and UTC days start on a slot boundary

    public static long SlotOf(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / SlotSeconds;
    public static DateTime StartOf(long slot) => DateTimeOffset.FromUnixTimeSeconds(slot * SlotSeconds).UtcDateTime;
    public static long DayOf(long slot) => slot / SlotsPerDay;

    /// <summary>Uniform [0,1) from (pet, slot, purpose): stable across processes and machines (SHA-256, not string.GetHashCode).</summary>
    internal static double Roll(Guid petId, long slot, string purpose)
    {
        Span<byte> input = stackalloc byte[16 + 8 + 16];
        petId.TryWriteBytes(input);
        BitConverter.TryWriteBytes(input[16..], slot);
        var p = System.Text.Encoding.ASCII.GetBytes(purpose);
        Span<byte> data = new byte[24 + p.Length];
        input[..24].CopyTo(data);
        p.CopyTo(data[24..]);
        var hash = SHA256.HashData(data);
        return (BitConverter.ToUInt64(hash, 0) >> 11) / (double)(1UL << 53);
    }

    private static int Points(IReadOnlyDictionary<string, int> skills, string key) => skills.TryGetValue(key, out var v) ? v : 0;

    /// <summary>Weight of each activity: idle is the default, every skill point makes its own activity more likely.</summary>
    public static (string Key, int Weight)[] Weights(IReadOnlyDictionary<string, int> skills) =>
    [
        (Activities.Idle, 6),
        (Activities.Walk, 1 + 2 * Points(skills, "exploration")),
        (Activities.Read, 1 + 2 * Points(skills, "wisdom")),
        (Activities.Hum, 1 + 2 * Points(skills, "creativity")),
        (Activities.Letter, 1 + 2 * Points(skills, "empathy")),
    ];

    public static string ActivityAt(Guid petId, IReadOnlyDictionary<string, int> skills, long slot)
    {
        var w = Weights(skills);
        var pick = Roll(petId, slot, "activity") * w.Sum(x => x.Weight);
        foreach (var (key, weight) in w)
        {
            if ((pick -= weight) < 0) return key;
        }
        return Activities.Idle;
    }

    public static ActivityInfo ActivityNow(Guid petId, IReadOnlyDictionary<string, int> skills, DateTime now)
    {
        var slot = SlotOf(now);
        return new ActivityInfo(ActivityAt(petId, skills, slot), StartOf(slot), StartOf(slot + 1));
    }

    /// <summary>Does the (finished) slot end with a find? Only walking can; the daily cap is applied by the caller.</summary>
    public static bool FindsSomething(Guid petId, IReadOnlyDictionary<string, int> skills, long slot, LifeOptions o) =>
        ActivityAt(petId, skills, slot) == Activities.Walk && Roll(petId, slot, "find") < o.FindChancePerWalk;

    /// <summary>Which item: rarity first (exploration shifts weight to the rarer ones), then uniformly among that rarity.</summary>
    public static string PickItem(Guid petId, IReadOnlyDictionary<string, int> skills, long slot, IReadOnlyList<(string ItemId, string Rarity)> catalogue)
    {
        var e = Points(skills, "exploration");
        (string Rarity, int Weight)[] rarities = [("common", 70), ("rare", 25 + 3 * e), ("precious", 5 + e)];
        var byRarity = rarities.Where(r => catalogue.Any(c => c.Rarity == r.Rarity)).ToArray();
        var pick = Roll(petId, slot, "rarity") * byRarity.Sum(r => r.Weight);
        var chosen = byRarity[^1].Rarity;
        foreach (var (rarity, weight) in byRarity)
        {
            if ((pick -= weight) < 0) { chosen = rarity; break; }
        }
        var pool = catalogue.Where(c => c.Rarity == chosen).Select(c => c.ItemId).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return pool[(int)(Roll(petId, slot, "item") * pool.Length)];
    }
}
