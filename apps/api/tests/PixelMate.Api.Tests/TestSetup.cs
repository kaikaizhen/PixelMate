using System.Runtime.CompilerServices;

namespace PixelMate.Api.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init() => Environment.SetEnvironmentVariable("PIXELMATE_SKIP_DOTENV", "1");   // runs before Program is loaded
}
