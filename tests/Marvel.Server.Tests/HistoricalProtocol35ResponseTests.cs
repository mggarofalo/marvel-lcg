using System.Text;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class HistoricalProtocol35ResponseTests
{
    [Theory]
    [InlineData(34)]
    [InlineData(36)]
    public void OtherHistoricalVersionsCannotUseTheProjectionNormalization(int version)
    {
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes($"{{\"version\":{version}}}")));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[{\"active\":true}]")]
    public void UnexpectedGroupDataCannotBeErasedByTheComparisonReader(string groups)
    {
        Assert.Throws<InvalidDataException>(() => HistoricalProtocol35Response.Read(
            Encoding.UTF8.GetBytes("{\"version\":35,\"world\":{\"encounter_groups\":" + groups + "}}")));
    }
}
