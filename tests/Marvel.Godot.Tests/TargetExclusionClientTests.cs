using Marvel.Rules.Prompts;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TargetExclusionClientTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IncompleteOrUnofferedExclusionsCannotEnterTheClientDraft(bool missingSet)
    {
        EngineResponse opened = Host().Exchange(EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        Affordance option = opened.Prompt!.Affordances[0];
        var targets = new TargetRequest([1, 2, 3], 1, 3)
        { ExclusiveSets = missingSet ? [null!] : [[1, 99]] };
        var response = opened with { Prompt = opened.Prompt with { Affordances = [option with { Targets = targets }] } };

        ClientStartupResult result = await new LocalGameClient(new FixedTransport(response))
            .OpenAsync(Specification(), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error?.Code);
    }
}
