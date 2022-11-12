// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();
//
// app.MapGet("/", () => "Hello World!");
//
// app.Run();

using Serilog;

public class Program
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

            var server = new Server();

            app.Map("/", context => context.Response.WriteAsync("Hello"));
            app.Map("/ws", server.Handle);

            Log.Information("Start listening on {Address}", address);
            
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