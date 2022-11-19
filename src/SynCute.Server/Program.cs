using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Serilog;
using SynCute.Common.Helpers;

namespace SynCute.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = ConfigApplication();

        try
        {
            if (!ConfigServer(configuration, out var app, args)) return;

            await app?.RunAsync()!;
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

    private static bool ConfigServer(IConfiguration configuration, out WebApplication? app, string[] args)
    {
        var (isSuccess, repoPath, token, port) = CheckCommandLine(args);
        if (!isSuccess)
        {
            app = null;
            return false;
        }

        ExtractOptions(configuration, port, token, ref repoPath, out var accessToken, out var hostPort);

        Log.Information("Configs:");
        Log.Information("Repo Path: {Path}", repoPath);
        Log.Information("Host Port: {Port}", hostPort);
        
        IResourceHelper resourceHelper = new ResourceHelper(repoPath);
        resourceHelper.CheckRepository();
        
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, hostPort);
        });
        
        builder.Services.AddRazorPages();
        builder.Services.AddControllersWithViews();
        
        app = builder.Build();
        app.UseWebSockets();

        var cs = new CancellationTokenSource();
        var server = new Core.Server(resourceHelper, accessToken, cs.Token);

        Console.CancelKeyPress += (_, _) =>
        {
            Log.Information("Going to stop application");
            cs.Cancel();
        };

        app.Map("/ws", server.Handle);
        app.Map("/status", server.HandleStatus);
        
        var options = new DefaultFilesOptions();
        options.DefaultFileNames.Clear();
        options.DefaultFileNames.Add("index.html");
        
        app.UseDefaultFiles(options);
        app.UseStaticFiles();

        app.UseAuthorization();

        app.MapDefaultControllerRoute();
        app.MapRazorPages();

        return true;
    }

    private static void ExtractOptions(IConfiguration configuration, int? port, string? token,
        [AllowNull] ref string repoPath, out string accessToken, out int hostPort)
    {
        repoPath ??= configuration["RepositoryPath"];
        if (port is null)
        {
            if (!int.TryParse(configuration["HostPort"], NumberStyles.Any, new NumberFormatInfo(), out hostPort))
            {
                throw new Exception("Host port is missing");
            }
        }
        else
        {
            hostPort = (int)port;
        }
        
        if (token is null)
        {
            accessToken = configuration["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Access token is missing");
            }
        }
        else
        {
            accessToken = token;
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
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        
        Console.Title = "SynCute Server";
        
        return configuration;
    }

    private static (bool success, string? repoPath, string? accessToken, int? hostPort) CheckCommandLine(string[] args)
    {
        string? repoPath = null;
        string? accessToken = null;
        int? hostPort = null;
        
        var showHelp = false;

        var p = new OptionSet()
        {
            {
                "r|repo=", "Set RepositoryPath, by default application reading it from appsettings.json.",
                r => repoPath = r
            },
            {
                "t|token=", "Set access token, by default application reading it from appsettings.json.",
                t => accessToken = t
            },
            {
                "p|port=",
                "Host port for listening, by default application reading it from appsettings.json.",
                (int p) => hostPort = p
            },
            {
                "h|help", "Show help",
                _ => showHelp = true
            },
        };

        List<string> extra;
        try
        {
            extra = p.Parse(args);
        }
        catch (OptionException e)
        {
            Console.Write("Error: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `--help' for more information.");
            return (false, null, null, null);
        }

        if (showHelp)
        {
            ShowHelp(p);
            return (false, null, null, null);
        }

        if (extra.Count <= 0) return (true, repoPath, accessToken, hostPort);
        
        var message = string.Join(" ", extra.ToArray());
        Log.Information("Using new message: {Message}", message);
        return (true, repoPath, accessToken, hostPort);
    }

    private static void ShowHelp (OptionSet p)
    {
        Console.WriteLine ("Usage: [OPTIONS]+ message");
        Console.WriteLine ();
        Console.WriteLine ("Options:");
        p.WriteOptionDescriptions (Console.Out);
    }
}