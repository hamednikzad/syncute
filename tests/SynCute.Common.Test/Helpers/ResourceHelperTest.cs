using FluentAssertions;
using SynCute.Common.Helpers;
using Xunit;

namespace SynCute.Common.Test.Helpers;

public class ResourceHelperTest
{
    [Fact]
    public void RemoveExtraFromPath_Should_Remove_Parent_Path_And_Convert_BackSlashes_To_Slashes()
    {
        const string repoPath = @"C:\Projects\Jobs\syncute\src\SynCute.Server\bin\Debug\net6.0\Repository";
        const string path = @"C:\Projects\Jobs\syncute\src\SynCute.Server\bin\Debug\net6.0\Repository\AnotherDirectory\Newtonsoft.Json.dll";
        var expected = @"/AnotherDirectory/Newtonsoft.Json.dll";
        
        IResourceHelper sut = new ResourceHelper(repoPath);
        
        var actual = sut.RemoveExtraFromPath(path);

        actual
            .Should()
            .BeEquivalentTo(expected);

    }
}