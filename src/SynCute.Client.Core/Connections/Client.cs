using System.Net.WebSockets;
using System.Text;
using Serilog;
using SynCute.Client.Core.Messages;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Models;

namespace SynCute.Client.Core.Connections;

public class Client : IDisposable
{
    private readonly IResourceHelper _resourceHelper;
    private readonly string _remoteAddress;
    private readonly string _accessToken;
    private readonly CancellationToken _cancellationToken;
    private readonly ClientMessageProcessor _messageProcessor;
    private ClientWebSocket _socket = null!;
    private bool _isOpen;

    private List<Resource> _currentResources;
    
    public Client(IResourceHelper resourceHelper, string remoteAddress, string accessToken, CancellationToken cancellationToken)
    {
        _resourceHelper = resourceHelper;
        _remoteAddress = remoteAddress;
        _accessToken = accessToken;
        _cancellationToken = cancellationToken;

        _messageProcessor = new ClientMessageProcessor(_resourceHelper, Send, Send);
        _currentResources = new List<Resource>();
    }

    #region Repository Related

    
    private void ConfigRepositoryWatcher()
    {
        var fileSystemWatcher = new FileSystemWatcher(_resourceHelper.GetRepositoryPath());
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.Created += async (_, args) =>{ await RepositoryModified(args);}; 
        fileSystemWatcher.Renamed += async (_, args) => { await RepositoryModified(args);};
        fileSystemWatcher.Deleted += async (_, args) => { await RepositoryModified(args);};
        fileSystemWatcher.Changed += async (_, args) => { await RepositoryModified(args);};
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    private async Task RepositoryModified(FileSystemEventArgs args)
    {
        switch (args.ChangeType)
        {
            case WatcherChangeTypes.Created: // We just use this event because other events are redundant
                Log.Information("Repository changed, Change: {ChangeType}, Name: {Name}", args.ChangeType, args.Name);
                var attr = File.GetAttributes(args.FullPath);

                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    if (_currentResources.Any(r => r.FullPath == args.FullPath))
                    {
                        Log.Information("Watcher: file {Name} already synced", args.FullPath);
                        return;
                    }
                    
                    await _messageProcessor.UploadResources(new List<Resource>()
                    {
                        _resourceHelper.GetResourceByFullPath(args.FullPath)
                    });
                }
                break;
            case WatcherChangeTypes.Deleted:
                break;
            case WatcherChangeTypes.Changed:
                break;
            case WatcherChangeTypes.Renamed:
                break;
            case WatcherChangeTypes.All:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

/* 
    private List<Resource> _lastResources = null!;
    private List<Resource> CompareResources(List<Resource> currentResources)
    {
        var result = new List<Resource>();

        foreach (var currentResource in currentResources)
        {
            var found = _lastResources.Any(r => r.Checksum == currentResource.Checksum &&
                                                          r.RelativePath == currentResource.RelativePath);
            if (found)
            {
                continue;
            }
            result.Add(currentResource);
        }

        return result;
    }
*/

    #endregion
    
    private async Task Send(string message)
    {
        await _socket.SendAsync(ArrayHelper.GetByteArray(message), WebSocketMessageType.Text,
            true, _cancellationToken);
    }

    private async Task Send(ReadOnlyMemory<byte> message, bool endOfMessage)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary,
            endOfMessage, _cancellationToken);
    }

    private async Task WaitForReceive()
    {
        Log.Information("Waiting for new message...");

        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            result = await _socket.ReceiveAsync(buffer, _cancellationToken);
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        switch (result.MessageType)
        {
            case WebSocketMessageType.Text:
                await ProcessTextMessage(ms);
                break;
            case WebSocketMessageType.Binary:
                await ProcessBinaryMessage(ms);
                break;
            case WebSocketMessageType.Close:
                _isOpen = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private ClientWebSocket CreateSocket()
    {
        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("access_token", _accessToken);
        return _socket;
    }
    
    private async Task Connect()
    {
        while (true)
        {
            try
            {
                Log.Information("Connecting to {Address}...", _remoteAddress);
                
                _socket = CreateSocket();
                await _socket.ConnectAsync(new Uri(_remoteAddress), _cancellationToken);
                _isOpen = true;
                Log.Information("Successfully connected");
                return;
            }
            catch (WebSocketException e)
            {
                Log.Error(e, "Error in connection to server");
                if (e.WebSocketErrorCode == WebSocketError.NotAWebSocket)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error in connection to server");
                throw;
            }

            const int sleep = 5000;
            Log.Information("Try to connect in {Sleep}ms", sleep);
            Thread.Sleep(sleep);
        }
    }

    public async Task Start()
    {
        _currentResources = _resourceHelper.GetAllFilesWithChecksum();
        
        ConfigRepositoryWatcher();
        
        await Connect();
                  
        while (_isOpen && _socket.State == WebSocketState.Open)
        {
            await WaitForReceive();
        }
    }

    private async Task ProcessBinaryMessage(MemoryStream ms)
    {
        var newResource = await _resourceHelper.WriteResource(ms);
        
        _currentResources.Add(newResource);
    }

    public void Dispose()
    {
        _isOpen = false;
        _socket.Dispose();
    }

    private async Task ProcessTextMessage(Stream ms)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        Log.Information("Receive message from server:{Msg}", text);

        var message = MessageDeserializer.Deserialize(text);

        await _messageProcessor.Process(message);
    }

    public async Task Stop()
    {
        if (!_isOpen)
            return;

        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing...", _cancellationToken);
        _isOpen = false;
    }
}