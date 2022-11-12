using System.Net;

public class Server
{
    private Dictionary<Guid, SocketConnection> _connections;

    public Server()
    {
        _connections = new Dictionary<Guid, SocketConnection>();
    }

    public async Task Handle(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var connection = new SocketConnection(webSocket);
            _connections.Add(connection.Id, connection);
            await connection.Handle();
            return;
        }

        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
    }
}