using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WorthySearchContextReviewTests
{
    [Theory]
    [InlineData("{\"areas\":[\"yourDeck\",\"yourDiscardPile\"],\"title\":\"Mjolnir\",\"kind\":\"Upgrade\"}", "Search your deck and discard pile for the Mjolnir upgrade and add it to your hand")]
    [InlineData("{\"areas\":[\"yourDiscardPile\",\"yourDeck\"],\"title\":\"Mjolnir\"}", "Search your deck and discard pile for Mjolnir and add it to your hand")]
    [InlineData("{\"area\":\"yourDeck\",\"top\":3,\"kind\":\"Upgrade\"}", "Search the top 3 cards of your deck for an upgrade and add it to your hand")]
    [InlineData("{\"area\":\"yourDiscardPile\",\"kind\":\"Upgrade\"}", null)]
    [InlineData("{\"area\":\"encounterDeck\",\"kind\":\"Upgrade\"}", null)]
    [InlineData("{\"areas\":[\"yourDeck\",\"yourHand\"],\"kind\":\"Upgrade\"}", null)]
    [InlineData("{\"area\":\"yourDeck\",\"title\":\"Mjolnir\",\"trait\":\"ASGARD\"}", null)]
    [InlineData("{\"area\":\"yourDeck\",\"title\":\"Mjolnir\",\"cardClass\":\"Hero\"}", null)]
    public void SyntheticSearchDescriptionPreservesExtentAndRefusesPartialCriteria(string fields, string? expected)
    {
        // Synthetic description contract; these selectors do not admit new product content.
        var selector = "{\"cardsIn\":" + fields + "}";
        Assert.Equal(expected, DescribeSynthetic(selector).DisplayLabel);
    }

    [Theory]
    [InlineData(false, "Search the top 3 cards of your deck for an ASGARD upgrade and add it to your hand")]
    [InlineData(true, "Search the top 3 cards of your deck for an upgrade without the ASGARD trait and add it to your hand")]
    public void SyntheticTraitWrapperRetainsBoundedExtent(bool exclude, string expected)
    {
        string operation = exclude ? "withoutTrait" : "withTrait";
        string selector = "{\"" + operation + "\":{\"cards\":{\"cardsIn\":{\"area\":\"yourDeck\",\"top\":3,\"kind\":\"Upgrade\"}},\"trait\":\"ASGARD\"}}";
        Assert.Equal(expected, DescribeSynthetic(selector).DisplayLabel);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualWorthyQueriesPreserveRandomAndPendingExecutionAgainstControl(bool matching)
    {
        var probed = ThorActualFixture.Board("06001b");
        var control = ThorActualFixture.Board("06001b");
        if (matching)
        {
            probed.CreateCard("06009", probed.Seats[0].Deck);
            control.CreateCard("06009", control.Seats[0].Deck);
        }
        var runner = AuthoredCards.Runner();
        string before = Snapshot(probed);
        for (int index = 0; index < 3; index++)
        {
            var action = Assert.Single(runner.Actions(probed, 0), option => option.Card == probed.Seats[0].IdentityCard.ObjectId);
            Assert.Contains("deck and discard pile", runner.Describe(probed, action).DisplayLabel);
        }
        Assert.Equal(before, Snapshot(probed));
        foreach (var world in new[] { probed, control })
        {
            var action = Assert.Single(runner.Actions(world, 0), option => option.Card == world.Seats[0].IdentityCard.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [], []));
        }
        Assert.Equal(Snapshot(control), Snapshot(probed));
        if (matching)
        {
            var pending = Assert.Single(probed.Agenda.Outstanding);
            before = Snapshot(probed);
            for (int index = 0; index < 3; index++)
                Assert.Equal("Odinson: choose the Mjolnir upgrade to add to your hand", runner.Choosing(probed, pending)!.DisplayQuestion);
            Assert.Equal(before, Snapshot(probed));
            foreach (var world in new[] { probed, control })
            {
                var step = Assert.Single(world.Agenda.Outstanding);
                var prompt = Sequence.Work(world, world.Facts, runner, [])!;
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), []);
            }
        }
        Sequence.Finish(probed, probed.Facts, runner, []);
        Sequence.Finish(control, control.Facts, runner, []);
        Assert.Equal(Snapshot(control), Snapshot(probed));
    }

    private static Affordance DescribeSynthetic(string selector)
    {
        string effect = "{\"if\":{\"test\":{\"exists\":" + selector + "},\"then\":{\"chooseCard\":{\"from\":" + selector
            + ",\"effect\":{\"seq\":[{\"addToHand\":\"chosen\"},{\"shuffle\":\"yourDeck\"}]}}},\"else\":{\"shuffle\":\"yourDeck\"}}}";
        var runner = Runner("06001b", "Action", effect);
        var world = ThorActualFixture.Board("06001b");
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == world.Seats[0].IdentityCard.ObjectId);
        return runner.Describe(world, action);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });
}
