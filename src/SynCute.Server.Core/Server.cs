using System.Net;
using Serilog;
using SynCute.Common.Helpers;
using SynCute.Server.Core.Connections;

namespace SynCute.Server.Core;

public class Server
{
    private readonly IResourceHelper _resourceHelper;
    
    public Server(IResourceHelper resourceHelper, string accessToken, CancellationToken cancellationToken)
    {
        _resourceHelper = resourceHelper;
        
        _accessToken = accessToken;
        _cancellationToken = cancellationToken;
        _connections = new Dictionary<Guid, ServerSocketConnection>();
        _webConnections = new Dictionary<Guid, WebConnection>();
    }

    #region Server Connections

    private readonly Dictionary<Guid, ServerSocketConnection> _connections;
    private readonly string _accessToken;
    private readonly CancellationToken _cancellationToken;

    public async Task Handle(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var accessToken = context.Request.Headers["access_token"];

            if (accessToken != _accessToken)
            {
                Log.Information("Unauthorized access rejected");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Log.Information("Server accept connection on Thread {Thread}", Environment.CurrentManagedThreadId);

            var connection = new ServerSocketConnection(_resourceHelper, webSocket, _cancellationToken);

            connection.ConnectionClosed += async conId => await ConnectionOnConnectionClosed(conId);

            connection.NewResourceReceived += async (connectionId, resource) =>
                await OnNewResourceReceived(connectionId, resource);

            _connections.Add(connection.Id, connection);

            await Task.Factory.StartNew(async () => { await SendConnectionStatus(); }, _cancellationToken);

            await connection.Start();

            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }

    private async Task OnNewResourceReceived(Guid connectionId, string resource)
    {
        Log.Information("Server send message to others on Thread {Thread}", Environment.CurrentManagedThreadId);

        foreach (var connection in _connections.Where(c => c.Key != connectionId))
        {
            await connection.Value.SendNewResourceReceived(resource);
        }

        await SendMessageToAllWebConnections($"New resource received: {resource}");
    }

    private async Task ConnectionOnConnectionClosed(Guid connectionId)
    {
        _connections.Remove(connectionId);
        await SendMessageToAllWebConnections("A connection closed");
        await SendConnectionStatus();
    }

    #endregion

    #region Web Connection

    private readonly Dictionary<Guid, WebConnection> _webConnections;

    public async Task HandleStatus(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                Log.Information("Server accept web connection");

                var connection = new WebConnection(webSocket);

                connection.ConnectionClosed += async connectionId =>
                {
                    _webConnections.Remove(connectionId);
                    await SendConnectionStatus();
                };

                AddNewWebConnection(connection);

                await connection.Send("Hello from server");
                await SendMessageToOtherWebConnections(connection.Id, "New connection!");
                await SendConnectionStatus();

                await connection.Start();
                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error in HandleStatus");
            }
        }

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }

    private void AddNewWebConnection(WebConnection connection)
    {
        CheckWebConnections();
        
        _webConnections.Add(connection.Id, connection);
    }

    private void CheckWebConnections()
    {
        foreach (var webConnection in _webConnections)
        {
            if (!webConnection.Value.IsConnected())
            {
                _webConnections.Remove(webConnection.Key);
            }
        }
    }

    private async Task SendConnectionStatus()
    {
        CheckWebConnections();
        await SendMessageToAllWebConnections($"Number of online connections: {_connections.Count}");
    }

    private async Task SendMessageToOtherWebConnections(Guid sourceId, string message)
    {
        CheckWebConnections();
        
        foreach (var keyValuePair in _webConnections.Where(c => c.Key != sourceId))
        {
            await keyValuePair.Value.Send(message);
        }
    }

    private async Task SendMessageToAllWebConnections(string message)
    {
        CheckWebConnections();
        
        foreach (var connection in _webConnections.Values)
        {
            await connection.Send(message);
        }
    }

    #endregion
}