using Marvel.Protocol;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class KangNativeAssertions
{
    internal static void Verify(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        string[] expected = contract switch
        {
            "kang-2" => ["split", "acting", "departure", "join", "reunion"],
            "kang-3" => ["split"],
            _ => ["split", "retained"],
        };
        Assert.Equal(expected, server.KangCheckpoints);
        var world = server.Responses.Last(response => response.World is not null).World!;
        Assert.NotNull(world.Kang);
        if (contract != "kang-2")
        {
            Assert.Empty(mutations);
            return;
        }
        // The client answered the join prompt itself: one commitment, and the table later reunited.
        var join = server.Responses.Last(response => response.Prompt?.Trigger == KangJoinOperation.Join);
        Assert.Equal(join.Revision, Assert.Single(mutations).ExpectedRevision);
        Assert.Equal(3, world.Kang!.Stage);
        Assert.Single(world.GameAreas, area => area.PlayAreas.Count > 0 && area.Central);
    }
}
