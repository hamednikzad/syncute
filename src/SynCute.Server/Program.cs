using System.Globalization;
using System.Net;
using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        //if (CheckCommandLine(args)) return;

        var configuration = ConfigApplication();

        try
        {
            if (ConfigServer(configuration, out var app)) return;

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

    private static bool ConfigServer(IConfigurationRoot configuration, out WebApplication app)
    {
        var builder = WebApplication.CreateBuilder();
        if (!int.TryParse(configuration["HostPort"], NumberStyles.Any, new NumberFormatInfo(), out var hostPort))
        {
            throw new Exception("Host port is missing");
        }
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, hostPort);
        });

        app = builder.Build();
        app.UseWebSockets();

        var accessToken = configuration["AccessToken"];
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Access token is missing");
        }

        var cs = new CancellationTokenSource();
        var server = new Server(accessToken, cs.Token);

        Console.CancelKeyPress += async (sender, args) =>
        {
            Log.Information("Going to stop application");
            cs.Cancel();
        };

        app.Map("/", context => context.Response.WriteAsync("SynCute server is Up and Running!", cancellationToken: cs.Token));
        app.Map("/ws", server.Handle);


        ResourceHelper.CheckRepository();
        return false;
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

    private static bool CheckCommandLine(string[] args)
    {
        var verbose = 0;
        var names = new List<string>();
        bool show_help = false;
        int repeat = 1;

        var p = new OptionSet()
        {
            {
                "n|name=", "the {NAME} of someone to greet.",
                v => names.Add(v)
            },
            {
                "r|repeat=",
                "the number of {TIMES} to repeat the greeting.\n" +
                "this must be an integer.",
                (int v) => repeat = v
            },
            {
                "v", "increase debug message verbosity",
                v =>
                {
                    if (v != null) ++verbosity;
                }
            },
            {
                "h|help", "show this message and exit",
                v => show_help = v != null
            },
        };

        List<string> extra;
        try
        {
            extra = p.Parse(args);
        }
        catch (OptionException e)
        {
            Console.Write("greet: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `greet --help' for more information.");
            return true;
        }

        if (show_help)
        {
            ShowHelp(p);
            return true;
        }

        string message;
        if (extra.Count > 0)
        {
            message = string.Join(" ", extra.ToArray());
            Debug("Using new message: {0}", message);
        }
        else
        {
            message = "Hello {0}!";
            Debug("Using default message: {0}", message);
        }

        foreach (var name in names)
        {
            for (int i = 0; i < repeat; ++i)
                Console.WriteLine(message, name);
        }

        return false;
    }

    static void ShowHelp (OptionSet p)
    {
        Console.WriteLine ("Usage: greet [OPTIONS]+ message");
        Console.WriteLine ("Greet a list of individuals with an optional message.");
        Console.WriteLine ("If no message is specified, a generic greeting is used.");
        Console.WriteLine ();
        Console.WriteLine ("Options:");
        p.WriteOptionDescriptions (Console.Out);
    }
    static int verbosity;
    static void Debug (string format, params object[] args)
    {
        if (verbosity > 0) {
            Console.Write ("# ");
            Console.WriteLine (format, args);
        }
    }
}