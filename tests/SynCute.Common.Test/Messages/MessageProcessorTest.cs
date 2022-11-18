using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Models;
using Xunit;

namespace SynCute.Common.Test.Messages;

public class MessageProcessorTest
{
    [Fact]
    public async Task UploadResources_Should_Serialize_Resources_And_Send()
    {
        //Arrange
        var currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var filePath = Path.Combine(currentPath!, "some_file_for_test.txt");
        var resourceHelper = new ResourceHelper(currentPath!);
        
        var testResource = resourceHelper.GetResourceByFullPath(filePath);
        
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var expected = fileBytes.Length + 4 + ArrayHelper.GetByteArray(testResource.RelativePath).Length;
        
        //Act
        var actualBytes = 0;
        var sut = new MessageProcessor(s => Task.CompletedTask,
            (memory, b) =>
            {
                actualBytes += memory.Length;
                return Task.CompletedTask;
            });
        
        await sut.UploadResources(new List<Resource>
        {
            testResource
        });
        
        //Assert
        actualBytes.Should().Be(expected);
    }
}