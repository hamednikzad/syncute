using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Client;

public static class Program
{
    public static async Task Main()
    {
        var configuration = ConfigApplication();

        if (ConfigClient(configuration, out var cs, out var client)) return;

        Console.CancelKeyPress += async (sender, args) =>
        {
            Log.Information("Going to stop application");
            await client?.Stop()!;
            cs.Cancel();
        };
        
        try
        {
            ResourceHelper.CheckRepository();

            await client?.Start()!;
        }
        catch (Exception ex)
        {
            await client?.Stop()!;
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IConfigurationRoot ConfigApplication()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                true)
            //.AddJsonFile($"appsettings.{GetOs()}.json", optional: true, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        
        Console.Title = "SynCute Client";
        
        return configuration;
    }

    private static bool ConfigClient(IConfigurationRoot configuration, out CancellationTokenSource cs, out Connections.Client? client)
    {
        var accessToken = configuration["AccessToken"];
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Access token is missing");
        }

        var remoteAddress = configuration["RemoteAddress"];
        if (string.IsNullOrEmpty(remoteAddress))
        {
            throw new Exception("RemoteAddress is missing");
        }

        cs = new CancellationTokenSource();
        client = new Connections.Client(remoteAddress, accessToken, cs.Token);
        return false;
    }
}