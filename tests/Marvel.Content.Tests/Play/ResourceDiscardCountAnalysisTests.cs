using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic count expressions exercise shared analysis before resource-discard effects commit.</summary>
public sealed class ResourceDiscardCountAnalysisTests
{
    [Theory]
    [InlineData("B", 1)]
    [InlineData("Y", 2)]
    public void PaidResourceCountRequiresDeclarationAndResolvesItsDeclaredBranch(string paidAs, int expected)
    {
        var runner = Runner("01004", "Action", """
            {"discardResourceIcons":{"if":{"test":{"paidWithResource":"B"},"then":1,"else":2}}}
            """);
        var (_, world) = Playing(_ => { }, hero: true, abilities: new NoCardAbilities());
        world.Abilities = runner;
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var source = world.CreateCard("01004", world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        var hand = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01003", world.Seats[0].Hand)).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.True(price.DeclarationSensitive);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [wild.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        runner.ExecuteAction(world, new PlayerAction(action, [wild.ObjectId], [],
            Allocations: [new ResourceAllocation(wild.ObjectId, Cost: 0, PaidAs: paidAs)]));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var option = Assert.Single(prompt.Affordances);
        var request = Assert.IsType<TargetRequest>(option.Targets);
        var chosen = hand.Take(expected).Select(card => card.ObjectId).ToArray();
        Assert.True(request.Allows(chosen));
        Assert.False(request.Allows(hand.Take(3 - expected).Select(card => card.ObjectId).ToArray()));
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(option.Id, chosen, []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3 - expected, world.Seats[0].Hand.Cards.Count);
        Assert.All(hand.Take(expected), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Theory]
    [InlineData("{\"discardRandomFromHand\":2}", "random payment receipt")]
    [InlineData("{\"discardFromHand\":2}", "physical receipt")]
    public void PrintedCountRejectsUnmodelledPhysicalCostReceiptsBeforeMutation(string cost, string reason)
    {
        var runner = Runner("01007", "Action", """
            {"discardResourceIcons":{"printedResourceIconsDiscarded":"game"}}
            """, cost: cost);
        var (_, world) = Playing(_ => { }, abilities: new NoCardAbilities());
        world.Abilities = runner;
        var source = InPlay(world, "01007");
        Hand(world, "01088", 3);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains(reason, error.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.True(source.Ready);
    }
}
