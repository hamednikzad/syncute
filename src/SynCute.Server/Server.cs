using System.Net;
using Serilog;
using SynCute.Server.Connections;

namespace SynCute.Server;

public class Server
{
    private readonly Dictionary<Guid, ServerSocketConnection> _connections;
    private readonly CancellationToken _cancellationToken;

    public Server(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _connections = new Dictionary<Guid, ServerSocketConnection>();
    }

    public async Task Handle(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Log.Information("Server accept connection on Thread {Thread}", Environment.CurrentManagedThreadId);

            var connection = new ServerSocketConnection(webSocket, _cancellationToken);

            connection.ConnectionClosed += ConnectionOnConnectionClosed;

            connection.NewResourceReceived += async (connectionId, resource) =>
                await OnNewResourceReceived(connectionId, resource);

            _connections.Add(connection.Id, connection);
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
    }

    private void ConnectionOnConnectionClosed(Guid connectionId)
    {
        _connections.Remove(connectionId);
    }
}