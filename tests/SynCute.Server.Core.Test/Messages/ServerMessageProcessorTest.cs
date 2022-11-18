using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;
using SynCute.Server.Core.Messages;
using Xunit;

namespace SynCute.Server.Core.Test.Messages;

public class ServerMessageProcessorTest
{
    private readonly IResourceHelper _resourceHelper;

    public ServerMessageProcessorTest()
    {
        var currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _resourceHelper = new ResourceHelper(currentPath!);
    }

    [Fact]
    public async Task Process_Should_Detect_BadMessage()
    {
        Message? actualMessage = null;
        var sut = new ServerMessageProcessor(_resourceHelper, json =>
            {
                actualMessage = MessageDeserializer.Deserialize(json);
                return Task.CompletedTask;
            },(memory, b) => Task.CompletedTask
        );
        
        await sut.Process(new BadMessage("Bad Message"));

        actualMessage.Should().BeOfType<BadMessage>();
    }

    [Fact]
    public async Task Process_Should_Return_PongMessage_InResponseOf_PingMessage()
    {
        Message? actualMessage = null;
        var sut = new ServerMessageProcessor(_resourceHelper, json =>
            {
                actualMessage = MessageDeserializer.Deserialize(json);
                return Task.CompletedTask;
            },(memory, b) => Task.CompletedTask
        );
        
        await sut.Process(new PingMessage());

        actualMessage.Should().BeOfType<PongMessage>();
    }

    [Fact]
    public async Task Process_Should_Return_AllResourcesListMessage_InResponseOf_GetAllResourcesMessage()
    {
        Message? actualMessage = null;
        var sut = new ServerMessageProcessor(_resourceHelper, json =>
            {
                actualMessage = MessageDeserializer.Deserialize(json);
                return Task.CompletedTask;
            },(memory, b) => Task.CompletedTask
        );
        
        await sut.Process(new GetAllResourcesMessage());

        actualMessage.Should().BeOfType<AllResourcesListMessage>();
    }
}