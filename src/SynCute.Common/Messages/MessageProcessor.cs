using System.Net.Sockets;
using Serilog;
using SynCute.Common.Helpers;
using SynCute.Common.Models;

namespace SynCute.Common.Messages;

public class MessageProcessor
{
    protected readonly Func<string, Task> Send;
    private readonly Func<ReadOnlyMemory<byte>, bool, Task> _sendByteArray;

    public MessageProcessor(Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray)
    {
        Send = send;
        _sendByteArray = sendByteArray;
    }

    public async Task UploadResources(List<Resource> resources)
    {
        if (!resources.Any())
        {
            Log.Information("There is nothing to upload");
            return;
        }
        
        foreach (var resource in resources)
        {
            await SendFile(resource);
        }
    }

    protected async Task DownloadResources(List<Resource> resources)
    {
        if (!resources.Any())
        {
            Log.Information("There is nothing to download");
            return;
        }

        await Send(MessageFactory
            .CreateDownloadResourcesJsonMessage(resources.Select(r => r.RelativePath).ToArray()));
    }
    
    private async Task SendFile(Resource resource)
    {
        var fileNameByte = ArrayHelper.GetByteArray(resource.RelativePath);
        var clientData = new byte[4 + fileNameByte.Length];
        var fileNameLen = ArrayHelper.GetByteArray(fileNameByte.Length);
        
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
                await _sendByteArray(bytesToSend, false);
                bytesSoFar += numBytes;
                var progress = (byte) (bytesSoFar * 100 / totalBytes);
                count++;
                if (progress >= lastStatus && progress != 100)
                {
                    Log.Information("{Count} File sending progress:{Progress}", count, progress);
                    
                }
                else if (progress >= 100)
                {
                    Log.Information("File sending completed {Count}", count);
                }
                else
                {
                    Log.Information("Unknown: Count:{Count}, progress:{Progress}, lastStatus: {LastStatus}", count,
                        progress, lastStatus);
                }
                lastStatus = progress;
            }
            await _sendByteArray(new ReadOnlyMemory<byte>(fileChunk, 0, 0), true);
 
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