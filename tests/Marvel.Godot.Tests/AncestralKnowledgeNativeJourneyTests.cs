using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AncestralKnowledgeNativeJourneyTests
{
    [Fact]
    public void NativeSeedReachesSixVisiblePhysicalChoicesThroughLegalSetupAndPayment()
    {
        // This is the legal opening used by ancestral_knowledge_smoke.gd, with
        // no injected cards, reordered deck, or replay-version conversion.
        Game game = DatasetGameFactory.Load(RepositoryPaths.Root)
            .Create(new GameSpecification("rhino", ["black_panther"], null, 4)).Game;
        Seat seat = game.State.Seats[0];
        Assert.Equal([13, 9, 31, 16, 17, 38], seat.Hand.Cards.Select(card => card.ObjectId));
        Assert.Equal("01042", game.State.Cards[9].FaceId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id, [13, 31, 16, 17, 38], []));
        Assert.Equal(PublicDecisionKind.CardSearch, game.Pending!.PublicKind);
        game.Resolve(Decision.Take(game.Pending.Affordances.Single(offer => offer.Label == "01048").Id));
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
        Affordance action = game.Pending.Affordances.Single(offer => offer.AnchorId == 9);
        Assert.Equal("Ancestral Knowledge", action.Label);
        Assert.Equal("01043b", game.State.Cards[11].FaceId);
        game.Resolve(Decision.Take(action.Id, [], [11]));

        Assert.Equal(PublicDecisionKind.VisibleCardSelection, game.Pending!.PublicKind);
        Affordance choice = Assert.Single(game.Pending.Affordances);
        Assert.Equal([13, 31, 16, 17, 38, 11], choice.Targets!.Legal);
        Assert.Equal("Shuffle selected cards into your deck", choice.CommitLabel);
        Assert.Contains("different titles", game.Pending.Description);
        Assert.False(choice.Targets.Allows([16, 17]));
        Assert.False(choice.Targets.Allows([13, 11]));
        var composer = new DecisionComposer(game.Pending);
        composer.SelectAffordance(choice.Id);
        Assert.Empty(composer.Targets);
        Assert.False(composer.Progress().IsReady);
        composer.SelectTargets([16, 31]);
        Assert.True(composer.TryBuild(out EngineDecision? answer, out _));
        Area discard = game.State.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        Assert.Equal(6, discard.Cards.Count);
        int deckBefore = seat.Deck.Cards.Count;
        game.Resolve(Decision.Take(answer!.Affordance, answer.Targets, answer.Resources ?? []));

        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
        Assert.Equal(deckBefore + 2, seat.Deck.Cards.Count);
        Assert.Contains(game.State.Cards[16], seat.Deck.Cards);
        Assert.Contains(game.State.Cards[31], seat.Deck.Cards);
        Assert.Equal([13, 17, 38, 11, 9], discard.Cards.Select(card => card.ObjectId));
    }
}
