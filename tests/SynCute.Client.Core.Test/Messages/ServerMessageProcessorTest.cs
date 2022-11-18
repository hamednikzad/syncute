using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SynCute.Client.Core.Messages;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;
using Xunit;

namespace SynCute.Client.Core.Test.Messages;

public class ClientMessageProcessorTest
{
    private readonly IResourceHelper _resourceHelper;

    public ClientMessageProcessorTest()
    {
        var currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _resourceHelper = new ResourceHelper(currentPath!);
    }

    [Fact]
    public async Task Process_Should_Detect_BadMessage()
    {
        Message? actualMessage = null;
        var sut = new ClientMessageProcessor(_resourceHelper, json =>
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
        var sut = new ClientMessageProcessor(_resourceHelper, json =>
            {
                actualMessage = MessageDeserializer.Deserialize(json);
                return Task.CompletedTask;
            },(memory, b) => Task.CompletedTask
        );
        
        await sut.Process(new PingMessage());

        actualMessage.Should().BeOfType<PongMessage>();
    }
    
    [Fact]
    public async Task Process_Should_Return_GetAllResourcesMessage_InResponseOf_ReadyMessage()
    {
        Message? actualMessage = null;
        var sut = new ClientMessageProcessor(_resourceHelper, json =>
            {
                actualMessage = MessageDeserializer.Deserialize(json);
                return Task.CompletedTask;
            },(memory, b) => Task.CompletedTask
        );
        
        await sut.Process(new ReadyMessage());

        actualMessage.Should().BeOfType<GetAllResourcesMessage>();
    }}