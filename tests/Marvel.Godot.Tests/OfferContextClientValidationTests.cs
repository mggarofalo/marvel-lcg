using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class OfferContextClientValidationTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData("missing", false)]
    [InlineData("negative", false)]
    [InlineData("empty", true)]
    [InlineData("public", true)]
    public async Task OfferContextRequiresExplicitNonnegativeIdentities(string shape, bool accepted)
    {
        EngineResponse opened = Host().Exchange(
            EngineRequest.OpenGame("local-open", "test-table", Specification()));
        Prompt prompt = opened.Prompt!;
        Affordance first = prompt.Affordances[0];
        IReadOnlyList<int> context = shape switch
        {
            "missing" => null!,
            "negative" => [-1],
            "empty" => [],
            "public" => [first.AnchorId],
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        Prompt changed = prompt with { Affordances = prompt.Affordances.Select(offer =>
            offer with { ContextCardIds = context }).ToArray() };
        var client = new LocalGameClient(new FixedTransport(opened with { Prompt = changed }));
        ClientEntryResult result = await client.OpenSessionAsync("test-table", Specification(),
            TestContext.Current.CancellationToken);
        Assert.Equal(accepted, result.Succeeded);
        if (!accepted) Assert.Equal("invalid_response", result.Error?.Code);
    }
}
