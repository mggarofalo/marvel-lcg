using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ChoiceThreatDescriptionTests
{
    [Theory]
    [InlineData("complete", true)]
    [InlineData("other-target", false)]
    [InlineData("other-removal", false)]
    [InlineData("unsupported-number", false)]
    public void OnlyTheCompletePublicChosenSchemeInstructionGetsAThwartQuantity(string shape, bool described)
    {
        // Synthetic instructions constrain subject and numeric disclosure;
        // they do not define additional playable card content.
        string chosen = "\"chosen\"";
        string main = """{"query":"mainScheme"}""";
        string target = shape == "other-target" ? main : chosen;
        string scheme = shape == "other-removal" ? main : chosen;
        string amount = shape == "unsupported-number" ? """{"add":[1,1]}""" : "2";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"chooseCard": {
                "from": {"query":"schemes"},
                "effect": {"thwart": {
                    "target": {{{{{{target}}}}}},
                    "effect": {"removeThreat": {
                        "scheme": {{{{{{scheme}}}}}}, "amount": {{{{{{amount}}}}}}
                    }}
                }}
            }}
            """);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        string before = world.Digest().Fingerprint();
        Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, false));
        string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
        Assert.Equal(described, description.Contains("Current thwart amount: 2 threat", StringComparison.Ordinal));
        Assert.DoesNotContain("Final step", description);
        Assert.Equal(before, world.Digest().Fingerprint());
    }
}
