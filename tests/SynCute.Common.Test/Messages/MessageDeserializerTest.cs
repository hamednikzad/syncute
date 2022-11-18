using System;
using FluentAssertions;
using SynCute.Common.Messages;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;
using Xunit;

namespace SynCute.Common.Test.Messages;

public class MessageDeserializerTest
{
    [Fact]
    public void Deserialize_Should_ReturnBadMessage()
    {
        var json = @"{'Type':'bad_message', 'Message':'Some Message'}";

        var actual = MessageDeserializer.Deserialize(json);
        
        actual
            .Should()
            .BeOfType<BadMessage>();
    }

    [Theory]
    [InlineData($@"{{'Type':'{BadMessage.CommandName}', 'Message':'Some Message'}}", typeof(BadMessage))]
    [InlineData($@"{{'Type':'{PingMessage.CommandName}'}}", typeof(PingMessage))]
    [InlineData($@"{{'Type':'{PongMessage.CommandName}'}}", typeof(PongMessage))]
    [InlineData($@"{{'Type':'{GetAllResourcesMessage.TypeName}'}}", typeof(GetAllResourcesMessage))]
    [InlineData($@"{{'Type':'{AllResourcesListMessage.TypeName}', 'Content':{{'Resources':[{{'Path':'Some Path', 'Checksum':'af5a366b-bcb0-40c0-a755-3896c2b2bf06 '}}]}}}}", typeof(AllResourcesListMessage))]
    [InlineData($@"{{'Type':'{DownloadResourcesMessage.TypeName}', 'Content':{{'Resources':['Some Path']}}}}", typeof(DownloadResourcesMessage))]
    [InlineData($@"{{'Type':'{NewResourceReceivedMessage.TypeName}', 'Content':{{'Resource':'Some Path'}}}}", typeof(NewResourceReceivedMessage))]
    public void Deserialize_Should_DetectExactType<T>(string json, Type expected)
    {
        var message = MessageDeserializer.Deserialize(json);
        var actual = message.GetType().ToString();
        
        actual
            .Should()
            .BeEquivalentTo(expected.ToString());
    }
}