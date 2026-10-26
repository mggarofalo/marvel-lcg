using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic controls for the bounded empty scheme-choice admission shape.</summary>
public sealed class InvocationEmptyChoiceReviewTests
{
    [Theory]
    [InlineData("direct")]
    [InlineData("nested")]
    [InlineData("dependent")]
    public void ValidSiblingDoesNotBroadenTheEmptyChoiceStructuralAllowance(string shape)
    {
        const string removal = """{"removeThreat":{"scheme":"chosen","amount":4}}""";
        string choiceBody = shape == "nested" ? $$$"""{"seq":[{{{removal}}}]}""" : removal;
        string choice = $$$"""{"chooseCard":{"from":{"query":"schemes"},"effect":{{{choiceBody}}}}}""";
        if (shape == "dependent")
            choice = "{\"then\":{\"effect\":" + choice + ",\"then\":{\"draw\":{\"player\":\"you\",\"count\":1}}}}";
        string body = $$$"""
            {"seq":[{{{choice}}},
              {"giveStatus":{"card":{"query":"villain"},"status":"confused"}}]}
            """;
        var runner = ActionAbilitySupportFixtures.Runner("01007", "Action", body, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            source = ActionAbilitySupportFixtures.InPlay(board, "01007");
            var main = board.TheCardIn(DeckType.MainSchemesArea)!;
            main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        }, hero: true, abilities: runner);
        string before = Snapshot(world);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.False(Statuses.Has(world, villain, Statuses.Confused));
        var offers = game.Pending!.Affordances.Where(offer => offer.AnchorId == source!.ObjectId).ToList();

        // The structural admission bound is an engine choice. Nested and dependent
        // choice bodies need their own supported admission rather than this allowance.
        if (shape != "direct")
        {
            Assert.Empty(offers);
            Assert.True(source!.Ready);
            Assert.Equal(before, Snapshot(world));
            return;
        }

        game.Resolve(Decision.Take(Assert.Single(offers).Id));

        Assert.False(source!.Ready);
        Assert.True(Statuses.Has(world, villain, Statuses.Confused));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
