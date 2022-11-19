# SynCute
SynCute is a .NET file synchronizer consisting of server and client programs. SynCute targets .NET 6.

## Server
SynCute server is an Asp.NET core application that serves WebSocket connections. Clients that are connected to the server sync their local directory with the server, and the server notifies other clients when a new resource is uploaded to the server.
Run server with --help for more information.

On the root of server address there is a web page that shows status of the server, realtime, which is implemented with WebSocket.

http://localhost:5000


## Docker
You can build docker file with this command:

`docker build --tag syncute-server -f DockerfileServer .`

## Client
SynCute client is a .NET console application that connects to the server and syncs local directory with the server and receive new resource from other clients through the server. Clients are not connected to each other, they just connected to the server.
Run client with --help for more information.

## Docker
You can build docker file with this command:

`docker build --tag syncute-client -f DockerfileClient .`

* Remember you should pass the remote server address to it.
