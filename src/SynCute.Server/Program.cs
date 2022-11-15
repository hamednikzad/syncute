using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Server;

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

        try
        {
            const string address = "http://localhost:6666";
            
            Console.Title = "Server";
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls();
            var app = builder.Build();
            app.UseWebSockets();

            var cs = new CancellationTokenSource();
            var server = new global::Server(cs.Token);
            
            Console.CancelKeyPress += async (sender, args) =>
            {
                Log.Information("Going to stop application");
                cs.Cancel();
            };
            
            app.Map("/", context => context.Response.WriteAsync("Hello", cancellationToken: cs.Token));
            app.Map("/ws", server.Handle);

            Log.Information("Start listening on {Address}", address);
            
            ResourceHelper.CheckRepository();
            
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}