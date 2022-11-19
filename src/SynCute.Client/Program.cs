using System.Diagnostics.CodeAnalysis;
using Serilog;
using SynCute.Common.Helpers;

namespace SynCute.Client;

public static class Program
{
    /// <summary>
    /// Entry Point
    /// </summary>
    /// <param name="args">-a ws://localhost:5000/ws -token SomeStrongToken -repo C:/repo</param>
    public static async Task Main(string[] args)
    {
        var configuration = ConfigApplication();

        if (!ConfigClient(configuration, out var cs, out var client, args)) return;

        Console.CancelKeyPress += async (_, _) =>
        {
            Log.Information("Going to stop application");
            await client?.Stop()!;
            cs?.Cancel();
        };

        try
        {
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

    private static bool ConfigClient(IConfiguration configuration,
        out CancellationTokenSource? cs, out Core.Connections.Client? client, string[] args)
    {
        client = null;
        cs = null;
        var (isSuccess, repoPath, token, address) = CheckCommandLine(args);
        if (!isSuccess)
        {
            return false;
        }

        ExtractOptions(configuration, ref repoPath, ref token, ref address);
        
        Log.Information("Configs:");
        Log.Information("Repo Path: {Path}", repoPath);
        Log.Information("Remote Address: {Address}", address);
        
        IResourceHelper resourceHelper = new ResourceHelper(repoPath);
        resourceHelper.CheckRepository();
        
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
        client = new Core.Connections.Client(resourceHelper, remoteAddress, accessToken, cs.Token);
        return true;
    }
    
    private static void ExtractOptions(IConfiguration configuration,
        [AllowNull] ref string repoPath, [AllowNull] ref string accessToken, [AllowNull] ref string remoteAddress)
    {
        repoPath ??= configuration["RepositoryPath"];
        accessToken ??= configuration["AccessToken"];
        remoteAddress ??= configuration["RemoteAddress"];
    }
    
    private static (bool success, string? repoPath, string? accessToken, string? remoteAddress) CheckCommandLine(string[] args)
    {
        string? repoPath = null;
        string? accessToken = null;
        string? address = null;

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
                "a|address=",
                "Set remote address for connecting to the server, by default application reading it from appsettings.json.",
                a => address = a
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

        if (extra.Count <= 0) return (true, repoPath, accessToken, address);

        var message = string.Join(" ", extra.ToArray());
        Log.Information("Using new message: {Message}", message);
        return (true, repoPath, accessToken, address);
    }

    private static void ShowHelp(OptionSet p)
    {
        Console.WriteLine("Usage: [OPTIONS]+ message");
        Console.WriteLine();
        Console.WriteLine("Options:");
        p.WriteOptionDescriptions(Console.Out);
    }
}