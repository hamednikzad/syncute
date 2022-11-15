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

            // app.UseMiddleware<WebSocketSecurityMiddleware>();
            
            var cs = new CancellationTokenSource();
            var server = new Server(cs.Token);
            
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

public class WebSocketSecurityMiddleware
{
    private readonly RequestDelegate _nextRequest;
	
    // stored access token usually retrieved from any storage
    // implemented thought OAuth or any other identity protocol
    private const string access_token = "821e2f35-86e3-4917-a963-b0c4228d1315";
	
    public WebSocketSecurityMiddleware(RequestDelegate next)
    {
        _nextRequest = next;
    }
	
    public async Task Invoke(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var accessToken = context.Request.Headers["access_token"];
	
            if (accessToken != access_token)
                throw new UnauthorizedAccessException();
                
            await _nextRequest.Invoke(context);
        }
        else
        {
            await _nextRequest.Invoke(context);
        }
    }
}