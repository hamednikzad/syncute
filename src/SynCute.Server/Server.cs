using System.Net;
using Serilog;
using SynCute.Server;

public class Server
{
    private readonly Dictionary<Guid, SocketConnection> _connections;

    public Server()
    {
        _connections = new Dictionary<Guid, SocketConnection>();
    }

    public async Task Handle(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Log.Information("Server accept connection on Thread {Thread}", Environment.CurrentManagedThreadId);

            var connection = new SocketConnection(webSocket);

            connection.ConnectionClosed += ConnectionOnConnectionClosed;

            connection.MessageReceived += async (connectionId, message) =>
                await ConnectionOnMessageReceived(connectionId, message);

            _connections.Add(connection.Id, connection);
            await connection.Start();
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }

    private async Task ConnectionOnMessageReceived(Guid connectionId, string message)
    {
        Log.Information("Server send message to Others on Thread {Thread}", Environment.CurrentManagedThreadId);

        foreach (var connection in _connections.Where(c => c.Key != connectionId))
        {
            await connection.Value.Send($"Message from {connectionId}: {message}");
        }
    }

    private void ConnectionOnConnectionClosed(Guid connectionId)
    {
        _connections.Remove(connectionId);
    }
}