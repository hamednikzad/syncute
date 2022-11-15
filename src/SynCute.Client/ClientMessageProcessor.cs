using System.Net.Sockets;
using System.Text;
using Serilog;
using SynCute.Core.Helpers;
using SynCute.Core.Messages;
using SynCute.Core.Messages.Behavioral;
using SynCute.Core.Messages.Resources;
using SynCute.Core.Models;

namespace SynCute.Client;

public class ClientMessageProcessor
{
    private readonly Func<string, Task> _send;
    private readonly Func<ReadOnlyMemory<byte>, bool, Task> _sendByteArray;

    public ClientMessageProcessor(Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray)
    {
        _send = send;
        _sendByteArray = sendByteArray;
    }

    public async Task Process(Message message)
    {
        switch (message)
        {
            case BadMessage badMessage:
                await OnBadMessage(badMessage);
                break;
            case PingMessage:
                await OnPingMessage();
                break;
            case PongMessage:
                OnPongMessage();
                break;
            case AllResourcesListMessage allResourcesListMessage:
                await OnAllResourcesListMessage(allResourcesListMessage);
                break;
            default:
                throw new Exception("Unknown message");
        }
    }

    private async Task OnBadMessage(BadMessage badMessage)
    {
        Log.Information("BadMessaged received: {Message}", badMessage.Message);
        await _send(MessageFactory.CreateBadJsonMessage(badMessage.Message));
    }

    private async Task OnPingMessage()
    {
        Log.Information("PingMessage received");
        await _send(MessageFactory.CreatePingJsonMessage());
    }

    private void OnPongMessage()
    {
        Log.Information("PongMessage received");
    }

    private async Task OnAllResourcesListMessage(AllResourcesListMessage message)
    {
        Log.Information("AllResourcesListMessage received");
        var serverResources = message.Content.Resources;
        var serverRelativePaths = serverResources.Select(r => r.RelativePath).ToList();

        var localResources = ResourceHelper.GetAllFilesWithChecksum();
        var localRelativePaths = localResources.Select(r => r.RelativePath).ToList();

        var shouldDownloads = serverResources.ExceptBy(localRelativePaths, r => r.RelativePath).ToList();
        var shouldUploads = localResources.ExceptBy(serverRelativePaths, r => r.RelativePath).ToList();
        var intersects = serverResources.IntersectBy(localRelativePaths, r => r.RelativePath).ToList();

        foreach (var resource in intersects)
        {
            if (localResources.Any(r => r.RelativePath == resource.RelativePath && r.Checksum != resource.Checksum))
            {
                shouldDownloads.Add(resource);
            }
        }

        await UploadResource(shouldUploads);

        // await DownloadResources(shouldDownloads);
    }

    private async Task DownloadResources(List<Resource> resources)
    {
        throw new NotImplementedException();
    }

    private async Task UploadResource(List<Resource> resources)
    {
        foreach (var resource in resources)
        {
            await SendFile(resource);
        }
    }

    private async Task SendFile(Resource resource)
    {
        var fileNameByte = Encoding.UTF8.GetBytes(resource.RelativePath);
        var clientData = new byte[4 + fileNameByte.Length];
        var fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
        fileNameLen.CopyTo(clientData, 0);
        fileNameByte.CopyTo(clientData, 4);
        await _sendByteArray(clientData, false);

        var lastStatus = 0;
        var file = new FileStream(resource.FullPath, FileMode.Open);
        long totalBytes = file.Length, bytesSoFar = 0;
        const int bufferSize = 8192;

        Log.Information("Sending file {File} with size {Size}", resource.RelativePath, totalBytes);
        try
        {
            var count = 0;
            var fileChunk = new byte[bufferSize];
            int numBytes;
            while ((numBytes = file.Read(fileChunk, 0, bufferSize)) > 0)
            {
                var bytesToSend = new ReadOnlyMemory<byte>(fileChunk, 0, numBytes);
                bytesSoFar += numBytes;
                var progress = (byte)(bytesSoFar * 100 / totalBytes);
                count++;
                if (progress > lastStatus && progress != 100)
                {
                    Log.Information("{Count} File sending progress:{Progress}", count, progress);
                    lastStatus = progress;
                    await _sendByteArray(bytesToSend, false);
                }
                else if (progress >= 100)
                {
                    Log.Information("File sending completed {Count}", count);
                    await _sendByteArray(bytesToSend, true);
                }
                else
                {
                    Log.Information("Unknown: Count:{Count}, progress:{progress}, lastStatus: {lastStatus}", count,
                        progress, lastStatus);
                }
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("Socket exception: {0}", e.Message);
        }
        finally
        {
            file.Close();
        }
    }
}