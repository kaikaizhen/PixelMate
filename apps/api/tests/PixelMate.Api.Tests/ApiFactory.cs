using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PixelMate.Api.Tests;

/// <summary>Boots the real pipeline with test configuration; no database is needed for these tests.</summary>
public sealed class ApiFactory(IDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=1;Database=x;Uid=x;Pwd=x;ConnectionTimeout=1",
            });
            if (settings is not null)
            {
                cfg.AddInMemoryCollection(settings);
            }
        });
    }
}
