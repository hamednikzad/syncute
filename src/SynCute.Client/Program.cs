using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Client;

public static class Program
{
    public static async Task Main()
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


        var cs = new CancellationTokenSource();
        var client = new Client(cs.Token);
        
        Console.CancelKeyPress += async (sender, args) =>
        {
            Log.Information("Going to stop application");
            await client.Stop();
            cs.Cancel();
        };
        
        try
        {
            Console.Title = "Client";

            ResourceHelper.CheckRepository();

            await client.Start();
        }
        catch (Exception ex)
        {
            await client.Stop();
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}