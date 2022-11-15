using System.Net.Sockets;
using System.Text;
using Serilog;
using SynCute.Core.Messages;
using SynCute.Core.Models;

namespace SynCute.Core;

public abstract class MessageProcessor
{
    protected readonly Func<string, Task> Send;
    private readonly Func<ReadOnlyMemory<byte>, bool, Task> _sendByteArray;

    protected MessageProcessor(Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray)
    {
        Send = send;
        _sendByteArray = sendByteArray;
    }

    protected async Task UploadResources(List<Resource> resources)
    {
        foreach (var resource in resources)
        {
            await SendFile(resource);
        }
    }

    protected async Task DownloadResources(List<Resource> resources)
    {
        await Send(MessageFactory
            .CreateDownloadResourcesJsonMessage(resources.Select(r => r.RelativePath).ToArray()));
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
                var progress = (byte) (bytesSoFar * 100 / totalBytes);
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