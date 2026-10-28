using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ConditionalResourceClientValidationTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData("G", 1UL, true)]
    [InlineData("?", 1UL, false)]
    [InlineData("", 1UL, false)]
    [InlineData("G", 0UL, false)]
    [InlineData("G", 2UL, false)]
    public async Task ConditionalFactsMustMatchTheOfferedCosts(string icons, ulong mask, bool valid)
    {
        EngineResponse complete = Host().Exchange(EngineRequest.OpenGame("local-open", "test-table", Specification()));
        var prompt = complete.Prompt!;
        var offer = prompt.Affordances[0];
        var source = new ResourceSource(7, "G") { ConditionalBonus = new(icons, mask) };
        var price = new CostOption(0, "1", Sources: [source]);
        var modified = complete with
        {
            Prompt = prompt with { Affordances = [offer with { Costs = [price] }] },
        };
        ClientEntryResult result = await new LocalGameClient(new FixedTransport(modified))
            .OpenSessionAsync("test-table", Specification(), TestContext.Current.CancellationToken);
        Assert.Equal(valid, result.Succeeded);
        if (valid) Assert.Null(result.Error);
        else Assert.Equal("invalid_response", result.Error?.Code);
    }
}
