using System.Net.WebSockets;
using System.Text;
using Serilog;

namespace SynCute.Client;

public class Program
{
    public static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
            //.AddJsonFile($"appsettings.{GetOs()}.json", optional: true, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        try
        {
            Console.Title = "Client";
            using (var ws = new ClientWebSocket())
            {
                await ws.ConnectAsync(new Uri("ws://localhost:6666/ws"), CancellationToken.None);
                var buffer = new byte[256];
                while (ws.State == WebSocketState.Open)
                {
                    Console.WriteLine("Waiting...");
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    }
                    else
                    {
                        Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, result.Count));
                    }
                }
            }
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