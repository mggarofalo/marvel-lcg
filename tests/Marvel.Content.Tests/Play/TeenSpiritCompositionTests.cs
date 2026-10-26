using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TeenSpiritCompositionTests
{
    [Fact]
    public void DependentDiscardLookupIsRefusedBeforeCostsOrHiddenDeckEffects()
    {
        // Synthetic composition: the first effect can add a second Energy to the
        // discard pile. Admission cannot know its future singular lookup result
        // without inspecting hidden deck contents, so it refuses the composition.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"seq":[
                {"discardUntil":{"from":"yourDeck","set":"ms_marvel","then":"addToHand"}},
                {"placeCounters":{"card":{"cardsIn":{"area":"yourDiscardPile","title":"Energy"}},"counter":"review","count":1}}
              ]}
            }]}]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            var removed = board.AreaOf(DeckType.RemovedArea, PlayArea.Of(0));
            foreach (var card in seat.Deck.Cards.Concat(seat.Hand.Cards).ToArray())
                World.MoveToTop(card, removed);
            var discard = board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            board.CreateCard("01088", discard);
            board.CreateCard("01089", seat.Deck);
            board.CreateCard("05003", seat.Deck);
            board.CreateCard("01088", seat.Deck);
            source = board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        });
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var prompt = game.Pending;
        bool busy = world.Agenda.IsBusy;
        Assert.Empty(runner.Actions(world, 0));
        Assert.Equal(before, world.Digest().Canonical());
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(forged, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Same(prompt, game.Pending);
        Assert.Equal(busy, world.Agenda.IsBusy);
        Assert.True(source.Ready);
    }
}
