using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class VisibleTargetGalleryTests
{
    [Fact]
    public void AncestralKnowledgePagesPhysicalCopiesAndKeepsSelectionWithoutMovingCardsBeforeCommit()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(catalog, 1) { Abilities = runner };
        Seat seat = world.CreateSeat("T'Challa");
        seat.IdentityCard = world.CreateCard("01040b", seat.Hero);
        world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        Card source = world.CreateCard("01042", seat.Hand);
        Card payment = world.CreateCard("01044", seat.Hand);
        world.CreateCard("01045", seat.Deck);
        Area discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        string[] faces = ["01046", "01047", "01048", "01049"];
        Card[] cards = [.. Enumerable.Range(0, 17).Select(index => world.CreateCard(faces[index % 4], discard))];
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [payment.ObjectId], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        Prompt prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        Assert.Equal(PublicDecisionKind.VisibleCardSelection, prompt.PublicKind);
        Assert.Contains("different titles", prompt.Description);
        Affordance offer = Assert.Single(prompt.Affordances);
        Assert.Equal("Shuffle selected cards into your deck", offer.CommitLabel);
        var composer = new DecisionComposer(prompt);
        composer.SelectAffordance(offer.Id);
        var operations = new TableDraftOperations(composer);
        var pages = new TargetCardPages();
        BoardPresentation board = BoardPresentation.From(WorldProjection.For(world, prompt, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, 1)).World);
        pages.Refresh(composer, 4);
        Assert.Equal(cards[0].ObjectId, pages.Current[0]);
        Assert.Equal(5, pages.Count);
        string before = world.Digest().Fingerprint();
        long words = world.Random.Generator.WordsConsumed;
        Assert.True(operations.CanToggleTarget(cards[0].ObjectId));
        Assert.True(operations.TryToggleTarget(cards[0].ObjectId));
        pages.Move(1);
        Assert.Equal(cards[4].ObjectId, pages.Current[0]);
        Assert.False(operations.CanToggleTarget(cards[4].ObjectId));
        Assert.False(operations.TryToggleTarget(cards[4].ObjectId));
        Assert.True(operations.TryToggleTarget(cards[5].ObjectId));
        // Inspection enumerates offered, already-readable faces; it does not own selection.
        Assert.Contains(pages.ReadableCards(board), card => card.TargetId == cards[0].ObjectId);
        pages.Refresh(composer, 4);
        Assert.Equal(1, pages.Page);
        Assert.Equal([cards[0].ObjectId, cards[5].ObjectId], composer.Targets);
        Assert.True(composer.Progress().IsReady);
        composer.SelectTargets([cards[0].ObjectId, cards[4].ObjectId]);
        Assert.False(composer.Progress().IsReady);
        Assert.False(composer.TryBuild(out _, out _));
        composer.SelectTargets([cards[0].ObjectId, cards[5].ObjectId]);
        Assert.Equal(before, world.Digest().Fingerprint());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        runner.Chose(world, source, 0, choice.Index, Decision.Take(offer.Id, composer.Targets.ToArray(), []), choice.Tier);
        Assert.Contains(cards[0], seat.Deck.Cards);
        Assert.Contains(cards[5], seat.Deck.Cards);
        Assert.Contains(cards[4], discard.Cards);
        Assert.True(world.Random.Generator.WordsConsumed > words);
    }

    [Fact]
    public void OneVisibleCandidateStillRequiresExplicitSelectionAndAReplacementPromptResetsPaging()
    {
        var composer = Draft([1]);
        Assert.Empty(composer.Targets);
        Assert.False(composer.UsesAutomaticTargetSelection);
        Assert.False(composer.Progress().IsReady);
        Assert.True(new TableDraftOperations(composer).TryToggleTarget(1));
        Assert.True(composer.Progress().IsReady);
        Assert.False(composer.UsesAutomaticTargetSelection);
        var pages = new TargetCardPages();
        pages.Refresh(Draft([1, 2, 3, 4, 5]), 2);
        pages.Move(2);
        Assert.Equal(2, pages.Page);
        pages.Refresh(composer, 2);
        Assert.Equal(0, pages.Page);
        int generation = 2;
        var stale = new TableDraftBinding(composer, 1, 0, (expected, _) => expected == generation);
        Assert.False(stale.CanToggleTarget(1));
        Assert.False(stale.TryToggleTarget(1));
        Assert.Equal([1], composer.Targets);
    }

    [Fact]
    public void ComparisonNeverAddsUnOfferedConcealedOrRemovedFaces()
    {
        var pages = new TargetCardPages();
        pages.Refresh(Draft([1, 2, 3]), 2);
        BoardCardPresentation Card(int id) => new(id, 1, false, "Same title", "", "UPGRADE", "", []);
        var board = new BoardPresentation([new(1, "Discard", "",
            [Card(1), Card(2) with { Concealed = true }, Card(4)], [Card(3)])]);
        Assert.Equal(1, Assert.Single(pages.ReadableCards(board)).TargetId);
    }

    private static DecisionComposer Draft(int[] ids)
    {
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "fixture", "Choose", false,
            [new Affordance(9, "Choose", 9, 0, "Choose", new TargetRequest(ids, 1, Math.Min(3, ids.Length)))])
        { PublicKind = PublicDecisionKind.VisibleCardSelection };
        var composer = new DecisionComposer(prompt);
        composer.SelectAffordance(9);
        return composer;
    }
}
