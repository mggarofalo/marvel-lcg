using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic characteristic-card probe; this does not admit the Black Widow pack.
public sealed class SafeHouseCostChoiceTests : ChoosingCardsTestBase
{
    internal const string Json = """
        {"cards":[{"card":"08005","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"alter-ego"},
          "cost":{"seq":[{"exhaust":"this"},{"chooseFromDiscard":"PREPARATION"}]},
          "effect":{"addToHand":"chosen"}
        }]}]}
        """;

    internal static AbilityRunner Runner() => new(AbilityCatalog.Parse(Json));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:initiating-abilities.step.5")]
    public void ExactNonfirstOwnPreparationIsChosenBeforeAtomicExhaustion(int seat)
    {
        // "Pay the cost(s)"; printed 08005 includes choosing before the arrow.
        var world = Deal("spider_man", "captain_marvel");
        var runner = Runner();
        world.Abilities = runner;
        var source = world.CreateCard("08005", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat), cardOwner: seat));
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat);
        var first = world.CreateCard("08006", discard);
        var second = world.CreateCard("08017", discard);
        var wrongTrait = world.CreateCard("01093", discard);
        var foreign = world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1 - seat), cardOwner: 1 - seat));
        var action = Assert.Single(runner.Actions(world, seat));
        var offer = runner.Describe(world, action);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, offer.Targets!.Legal);
        Assert.Equal(1, offer.Targets.Min);
        Assert.Equal(1, offer.Targets.Max);
        Assert.True(source.Ready);
        Assert.Contains("PREPARATION", offer.CostDescription);
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], [second.ObjectId]));
        Assert.True(result.IsComplete);
        Assert.False(source.Ready);
        Assert.Same(world.Seats[seat].Hand, second.Area);
        Assert.Same(discard, first.Area);
        Assert.Same(discard, wrongTrait.Area);
        Assert.NotEqual(world.Seats[seat].Hand, foreign.Area);
        Assert.DoesNotContain(runner.Actions(world, seat), next => next.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("wrong-trait")]
    [InlineData("foreign")]
    [InlineData("exhausted")]
    public void UnpayableCostIsNotOffered(string reason)
    {
        var world = Deal("spider_man", "captain_marvel");
        var runner = Runner();
        world.Abilities = runner;
        var source = world.CreateCard("08005", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        if (reason != "none")
            world.CreateCard(reason == "wrong-trait" ? "01093" : "08017",
                world.AreaOf(DeckType.DiscardPile, PlayArea.Of(reason == "foreign" ? 0 : 1), cardOwner: reason == "foreign" ? 0 : 1));
        if (reason == "exhausted") source.Exhaust();
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("foreign")]
    [InlineData("duplicate")]
    [InlineData("stale")]
    [Rule("rr:initiating-abilities.step.5")]
    public void RejectedAnswerPreservesPromptStateRandomAndPendingWork(string reason)
    {
        // If costs cannot be paid, "abort this process without paying any costs."
        Card source = null!;
        Card chosen = null!;
        Card other = null!;
        var runner = Runner();
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("08005", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            chosen = board.CreateCard("08017", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            other = board.CreateCard("08017", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == source.ObjectId);
        if (reason == "stale") World.MoveToTop(chosen, world.Seats[0].Hand);
        int[] targets = reason switch
        {
            "missing" => [], "invalid" => [source.ObjectId], "foreign" => [other.ObjectId],
            "duplicate" => [chosen.ObjectId, chosen.ObjectId], _ => [chosen.ObjectId],
        };
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var current = world.Agenda.Current;
        for (int retry = 0; retry < 2; retry++)
        {
            if (reason == "stale")
                Assert.Contains("no action this player can trigger",
                    Assert.Throws<RulesNotImplementedException>(() => game.Resolve(new Decision(offer.Id, targets))).Message);
            else Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, targets)));
            Assert.Same(prompt, game.Pending);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Equal(current, world.Agenda.Current);
            Assert.True(source.Ready);
        }
        if (reason == "stale") World.MoveToTop(chosen, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        game.Resolve(new Decision(offer.Id, [chosen.ObjectId]));
        Assert.False(source.Ready);
        Assert.Same(world.Seats[0].Hand, chosen.Area);
    }

    [Fact]
    public void RepeatedOfferQueriesPreserveCompleteRandomAndAgendaAndMatchUnprobedExecution()
    {
        var runner = Runner();
        var world = Deal("spider_man", "captain_marvel");
        var control = Deal("spider_man", "captain_marvel");
        foreach (var board in new[] { world, control })
        {
            board.Abilities = runner;
            board.CreateCard("08005", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            board.CreateCard("08017", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        }
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var current = world.Agenda.Current;
        for (int probe = 0; probe < 4; probe++)
            Assert.Single(runner.Describe(world, Assert.Single(runner.Actions(world, 1))).Targets!.Legal);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(current, world.Agenda.Current);
        foreach (var board in new[] { world, control })
        {
            var source = board.Cards.Single(card => card.FaceId == "08005");
            var selected = board.Cards.Single(card => card.FaceId == "08017");
            runner.ExecuteAction(board, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 1), [], [selected.ObjectId]));
        }
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Agenda.Outstanding, world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("\"draw\":{\"player\":\"you\",\"count\":1}")]
    [InlineData("\"addToHand\":\"this\"")]
    public void LoweringRefusesEffectsOutsideTheBoundedRecovery(string effect)
    {
        Assert.Throws<AbilityException>(() => RunnerFor(Json.Replace("\"addToHand\":\"chosen\"", effect)));
    }

    private static AbilityRunner RunnerFor(string json) => new(AbilityCatalog.Parse(json));

    [Theory]
    [InlineData(0, "01001a")]
    [InlineData(1, "01010a")]
    [Rule("rr:ability.8")]
    [Rule("rr:ability.13")]
    public void OnlyTheControllerInAlterEgoCanInitiate(int seat, string heroFace)
    {
        // "The player who controls the card ... determines whether or not to use"
        // it; an Alter-Ego trigger requires the player to be "in the specified form."
        var world = Deal("spider_man", "captain_marvel");
        var runner = Runner();
        world.Abilities = runner;
        var source = world.CreateCard("08005", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat), cardOwner: seat));
        world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat));
        Assert.Contains(runner.Actions(world, seat), action => action.Card == source.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1 - seat), action => action.Card == source.ObjectId);
        world.Seats[seat].IdentityCard.TurnTo(heroFace);
        Assert.DoesNotContain(runner.Actions(world, seat), action => action.Card == source.ObjectId);
        Assert.True(source.Ready);
    }
}
