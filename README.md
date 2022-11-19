# syncute
SynCute is a .NET file synchronizer consisting of server and client programs. SynCute targets .NET 6.

## Server
SynCute server is an Asp.NET core application that serves WebSocket connections. Clients that are connected to the server sync their local directory with the server, and the server notifies other clients when a new resource is uploaded to the server.
Run server with --help for more information.

## Client
SynCute client is a .NET console application that connects to the server and syncs local directory with the server and receive new resource from other clients throgh the server. Clients are not connected to each other, they just connected to the server.
Run client with --help for more information.