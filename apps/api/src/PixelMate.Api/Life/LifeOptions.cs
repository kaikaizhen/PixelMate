namespace PixelMate.Api.Life;

/// <summary>Numbers of the life rules (section "Life" in appsettings). Bump <see cref="RulesVersion"/> when they change.</summary>
public sealed class LifeOptions
{
    public int RulesVersion { get; set; } = 1;
    /// <summary>A pet finds at most this many items per UTC day.</summary>
    public int MaxFindsPerDay { get; set; } = 3;
    /// <summary>Chance that a finished walking slot ends with a find (about 2 a day for a pet with no exploration points).</summary>
    public double FindChancePerWalk { get; set; } = 0.2;
    /// <summary>Offline longer than this: only the most recent days are made up.</summary>
    public int CatchUpDays { get; set; } = 7;
    /// <summary>Background worker period; 0 turns the worker off (the API still catches up lazily).</summary>
    public int WorkerIntervalSeconds { get; set; } = 300;
    /// <summary>Local time of the pet world (decides sunrise / day / sunset / night). Taiwan by default.</summary>
    public int UtcOffsetHours { get; set; } = 8;
}
