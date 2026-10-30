using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Timing;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.InvocationTopDiscardFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual printed cycling and controller-sensitive healing on nonpublic engine boards.</summary>
public sealed class InvocationTopDiscardTests
{
    [Rule("rr:limit")]
    [Fact]
    public void NaturalTalentDiscardsOnceAndFormChangesRetainItsPhaseUse()
    {
        // "Each copy ... may be used X times per the specified period".
        var (world, game, deck, _, runner) = Board();
        var identity = world.Seats[1].IdentityCard;
        var top = deck.PublicTop!;
        var action = Action(game, identity);
        var result = game.Resolve(Decision.Take(action.Id));
        Assert.Same(deck.Discard, top.Area);
        Assert.Equal(4, deck.Deck.Cards.Count);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        identity.TurnTo("09001a");
        identity.TurnTo("09001b");
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.Contains(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        Assert.Contains(result.Events, happened => happened is CardsMoved);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void FullHealthWongCanDiscardFromTheUniqueDeckAndRecycleWithoutPenalty(int controller, bool last)
    {
        var (world, game, deck, wong, runner) = Board(controller, last);
        var top = deck.PublicTop!;
        var ordinary = world.Seats[controller].Deck.Cards.ToArray();
        var offer = Action(game, wong);
        var result = game.Resolve(Decision.Take(offer.Id));
        // Only the useful discard option is offered, then it completes without a second exhaustion.
        var choice = Assert.Single(game.Pending!.Affordances);
        Assert.Equal("discard", choice.Label);
        result = game.Resolve(Decision.Take(choice.Id));
        Assert.False(wong.Ready);
        Assert.Equal(1, wong.Owner);
        Assert.Equal(PlayArea.Of(controller), wong.Area.PlayArea);
        Assert.Equal(ordinary, world.Seats[controller].Deck.Cards);
        Assert.Equal(last ? 5 : 4, deck.Deck.Cards.Count);
        Assert.Equal(last ? 0 : 1, deck.Discard.Cards.Count);
        Assert.Same(last ? deck.Deck : deck.Discard, top.Area);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.DealtEncounterCardsDeck && area.Cards.Count > 0);
        Assert.DoesNotContain(runner.Actions(world, controller), item => item.Card == wong.ObjectId);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:you-your")]
    [Rule("rr:ownership-and-control.3")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void WongsHealFollowsHisControllerWhileTheOtherBranchUsesStrangesDeck(int controller)
    {
        // Control remains constant unless an ability changes it; "your identity" names the resolver's identity.
        var (world, game, deck, wong, _) = Board(controller, damaged: true);
        var top = deck.PublicTop;
        game.Resolve(Decision.Take(Action(game, wong).Id));
        Assert.False(wong.Ready);
        var choice = Assert.Single(game.Pending!.Affordances, offer => offer.Label == "heal");
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(1, world.Seats[controller].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1 - controller].IdentityCard.Damage);
        Assert.Same(top, deck.PublicTop);
        Assert.Empty(deck.Discard.Cards);
    }

    [Fact]
    public void QueriesPreserveRngPendingWorkAndSubsequentCompletion()
    {
        var (world, game, _, wong, runner) = Board(0, last: true);
        var (control, controlGame, _, controlWong, _) = Board(0, last: true);
        string before = SpellMasteryFixture.Snapshot(world);
        for (int i = 0; i < 4; i++)
            foreach (var action in runner.Actions(world, 0)) _ = runner.Describe(world, action);
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        var actual = game.Resolve(Decision.Take(Action(game, wong).Id));
        var expected = controlGame.Resolve(Decision.Take(Action(controlGame, controlWong).Id));
        actual = game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        expected = controlGame.Resolve(Decision.Take(Assert.Single(controlGame.Pending!.Affordances).Id));
        Assert.Equal(SpellMasteryFixture.Snapshot(control), SpellMasteryFixture.Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
    }
    [Fact]
    public void RejectedWongChoiceKeepsItsPromptAndAcceptedExhaustionAndRetryDiscardsTheCurrentTop()
    {
        var (world, game, deck, wong, _) = Board(0, damaged: true);
        game.Resolve(Decision.Take(Action(game, wong).Id));
        var prompt = game.Pending!;
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Decline));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Assert.False(wong.Ready);
        var top = deck.PublicTop!;
        game.Resolve(Decision.Take(Assert.Single(prompt.Affordances, item => item.Label == "discard").Id));
        Assert.Same(deck.Discard, top.Area);
        Assert.False(wong.Ready);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void HiddenOrMissingTopCannotProvideAnIneffectiveFullHealthAction()
    {
        var (world, _, deck, wong, runner) = Board(0, start: false);
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == wong.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == world.Seats[1].IdentityCard.ObjectId);
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Assert.Null(deck.PublicTop);
        var (live, _, liveDeck, liveWong, liveRunner) = Board(0);
        var aside = live.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        foreach (var card in liveDeck.Deck.Cards.ToArray()) World.MoveToTop(card, aside);
        Assert.DoesNotContain(liveRunner.Actions(live, 0), item => item.Card == liveWong.ObjectId);
        live.Seats[0].IdentityCard.TakeDamage(1);
        Assert.Contains(liveRunner.Actions(live, 0), item => item.Card == liveWong.ObjectId);
    }

    [Fact]
    public void AnUntypedFaceupInvocationLookalikeCannotDivertTheTypedDeckSelection()
    {
        // Synthetic additional piles do not establish the insert's five-card Invocation binding.
        var (world, game, deck, wong, _) = Board(0);
        var additional = world.CreateArea(DeckType.AdditionalDeck, 0, PlayArea.Of(0));
        var lookalike = world.CreateCard("09036", additional);
        lookalike.TurnFaceUp();
        var top = deck.PublicTop!;
        game.Resolve(Decision.Take(Action(game, wong).Id));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        Assert.Same(deck.Discard, top.Area);
        Assert.Same(additional, lookalike.Area);
        Assert.Single(additional.Cards);
        Assert.False(wong.Ready);
    }

    [Fact]
    public void AnUntypedFaceupInvocationLookalikeCannotProvideTheDiscardBranch()
    {
        // The same printed face in an ordinary additional pile is not a typed Invocation deck.
        var world = new World(InvocationTopDiscardFixture.Cards, 1, 5489);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001b,01001a", seat.Hero);
        var wong = world.CreateCard("09002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var additional = world.CreateArea(DeckType.AdditionalDeck, 0, PlayArea.Of(0));
        var lookalike = world.CreateCard("09036", additional);
        lookalike.TurnFaceUp();
        var runner = Runner();
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == wong.ObjectId);
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        seat.IdentityCard.TakeDamage(1);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == wong.ObjectId);
    }

    [Fact]
    public void AmbiguousTypedDecksFailClosedWithoutSelectingASeatOrChangingState()
    {
        // Synthetic ambiguity is outside legal unique-identity setup; the query must not pick the first deck.
        var (world, _, _, _, runner) = Board(0);
        var seat = world.Seats[0];
        seat.IdentityCard = world.CreateCard("09001b,09001a", seat.Hero);
        var aside = world.CreateArea(DeckType.AsideDeck, 0, PlayArea.Of(0));
        InvocationDeck.Initialize(world, 0,
            Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray(), "setup", []);
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
    }

    [Rule("rr:ownership-and-control.7.2")]
    [Fact]
    public void ActualMakeTheCallReturnsWongToAnotherControllerWhoCanCycleStrangesDeck()
    {
        // A departed ally uses its "owner's equivalent out-of-play area"; 01071 explicitly changes control.
        var (world, _, deck, wong, runner) = Board(0);
        Discard.Card(world, wong, "test", []);
        Assert.Equal(PlayArea.Of(1), wong.Area.PlayArea);
        var source = world.CreateCard("01071", world.Seats[0].Hand);
        var doubleResource = world.CreateCard("01088", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01003", world.Seats[0].Hand);
        var call = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        world.Abilities = call;
        call.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []));
        var callPrompt = Sequence.Work(world, InvocationTopDiscardFixture.Cards, call, [])!;
        var offered = Assert.Single(callPrompt.Affordances);
        Sequence.Answer(world, InvocationTopDiscardFixture.Cards, call, callPrompt, Decision.Take(wong.ObjectId, [], [doubleResource.ObjectId, singleResource.ObjectId],
            new Dictionary<string, long>(), ResourcePayment.Allocate(Assert.Single(offered.CostOptions),
                [doubleResource.ObjectId, singleResource.ObjectId])!), []);
        Assert.Null(Sequence.Work(world, InvocationTopDiscardFixture.Cards, call, []));
        Assert.Equal(PlayArea.Of(0), wong.Area.PlayArea);
        Assert.Equal(1, wong.Owner);
        Assert.True(wong.Ready);
        world.Abilities = runner;
        var top = deck.PublicTop!;
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0), item => item.Card == wong.ObjectId), [], []));
        var choice = Sequence.Work(world, InvocationTopDiscardFixture.Cards, runner, [])!;
        Sequence.Answer(world, InvocationTopDiscardFixture.Cards, runner, choice, Decision.Take(Assert.Single(choice.Affordances).Id), []);
        Assert.Null(Sequence.Work(world, InvocationTopDiscardFixture.Cards, runner, []));
        Assert.Same(deck.Discard, top.Area);
        Assert.False(wong.Ready);
    }

}
