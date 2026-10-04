using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PublicPendingSituationClientTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData("missing-causes")]
    [InlineData("unknown-purpose")]
    [InlineData("unknown-card")]
    [InlineData("unknown-seat")]
    public async Task MalformedPublicMetadataIsRejectedBeforeItCanReachTheRenderer(string problem)
    {
        EngineResponse opened = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        WorldDescriptor world = opened.World!;
        TableContextDescriptor table = world.Table!;
        PendingSituationDescriptor pending = table.PendingSituation!;
        TableContextDescriptor malformed = problem switch
        {
            "missing-causes" => table with { PendingSituation = pending with { SourceCardIds = null! } },
            "unknown-purpose" => table with { PendingSituation = pending with { Kind = (PublicDecisionKind)999 } },
            "unknown-card" => table with { PendingSituation = pending with { SourceCardIds = [999999] } },
            "unknown-seat" => table with { PromptOwner = 999 },
            _ => throw new ArgumentOutOfRangeException(nameof(problem)),
        };

        ClientStartupResult result = await new LocalGameClient(new FixedTransport(opened with
            { World = world with { Table = malformed } })).OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error!.Code);
    }
}
