namespace PixelMate.Api.Tests;

public class DatabaseConfigTests
{
    /// <summary>
    /// The tests run on SQLite, which cannot show this: with MySQL's EnableRetryOnFailure, EF refuses every explicit transaction
    /// (gifts, pairing, the room editor) with a 500. Keep the retrying strategy out of the production configuration.
    /// </summary>
    [Fact]
    public void The_mysql_configuration_does_not_use_a_retrying_execution_strategy()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "PixelMate.Api", "Program.cs"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var program = File.ReadAllLines(Path.Combine(dir!.FullName, "src", "PixelMate.Api", "Program.cs"));
        Assert.DoesNotContain(program, line => line.Contains("EnableRetryOnFailure") && !line.TrimStart().StartsWith("//"));
    }
}
