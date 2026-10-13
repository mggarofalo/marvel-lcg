using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Content;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class OccurrenceFaceTests
{
    [Theory]
    [InlineData("01001a")]
    [InlineData("01002")]
    [InlineData("01097b")]
    [InlineData("01099")]
    public void OccurrenceReferenceRetainsTheCompleteCanonicalPrintedFace(string id)
    {
        CardCatalog facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 7);
        world.CreateSeat("Player");
        Card card = world.CreateCard(id, world.AreaOf(DeckType.EncounterDiscardPile));
        var happened = new CardsFlipped([card.ObjectId], true) { SubjectFaces = OccurrenceFaces.For(card) };
        VisibleResult visible = WorldProjection.For(world, null, [happened],
            new PermissiveVisibilityPolicy().Authorize(null, world.Players));
        BoardCardPresentation standard = BoardPresentation.From(visible.World).Areas.SelectMany(area => area.Cards)
            .Single(candidate => candidate.TargetId == card.ObjectId);
        BoardCardPresentation reference = visible.World.EventFaces[id];
        Assert.Equal(standard.PrintedStats, reference.PrintedStats);
        Assert.Equal(standard.PrintedMarks, reference.PrintedMarks);
        Assert.Equal(standard.RulesMarkup, reference.RulesMarkup);
        Assert.Equal(standard.Kind, reference.Kind);
        Assert.Equal(standard.Traits, reference.Traits);
        Assert.Null(reference.TargetId);
        Assert.Empty(reference.Fields);
        Assert.Empty(reference.EffectiveValues);
    }

    [Fact]
    public void RevealedThenDiscardedEncounterKeepsExactPrintedReferenceWithoutLiveState()
    {
        World world = VisibilityFixture.Board();
        Card card = world.AreaOf(DeckType.EncounterDeck).Cards[0];
        var events = new List<GameEvent>();
        EncounterRevealExposure.Expose(world, card, "test reveal", events);
        Discard.Card(world, card, "test reveal", events);
        VisibleResult visible = WorldProjection.For(world, null, events,
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        EventPresentation revealed = EventPresenter.Present(visible.Events, visible.World)[0];
        BoardCardPresentation face = Assert.IsType<BoardCardPresentation>(Assert.Single(revealed.Activity!.Subjects).Reference);
        Assert.Equal("secret-a", face.FaceId);
        Assert.Null(face.TargetId);
        Assert.Empty(face.Fields);
        Assert.Empty(face.EffectiveValues);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    [Fact]
    public void AbsentSideSchemeUsesTheAuthorizedOccurrenceOrientation()
    {
        var reference = new BoardCardPresentation(null, 1, false, "Breakin & Takin", "",
            "ENCOUNTER SIDE SCHEME", "", []) { FaceId = "01099" };
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished)
        { EventFaces = new Dictionary<string, BoardCardPresentation> { ["01099"] = reference } };
        var happened = new CardsFlipped([42], true)
        { SubjectFaces = new Dictionary<int, string> { [42] = "01099" } };
        ActivitySubject subject = Assert.Single(Assert.Single(EventPresenter.Present([happened], world))
            .Activity!.Subjects);
        Assert.True(subject.Landscape);
        Assert.Same(reference, subject.Reference);
    }

    [Fact]
    public void FinalReadableFaceDoesNotInventAnOccurrenceFaceGrant()
    {
        World world = VisibilityFixture.Board();
        Card card = world.AreaOf(DeckType.VillainArea).Cards[0];
        var events = new GameEvent[] { new FieldSet(card.ObjectId, "health", 7, 6) };
        VisibleResult visible = WorldProjection.For(world, null, events,
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        Assert.Empty(visible.World.EventFaces);
        Assert.Null(Assert.Single(EventPresenter.Present(visible.Events, visible.World)).Activity!.Subjects[0].Reference);
    }

    [Fact]
    public void OccurrenceIdentityDoesNotFollowLaterChangesToTheSameObject()
    {
        World world = VisibilityFixture.Board();
        Card card = world.AreaOf(DeckType.VillainArea).Cards[0];
        var happened = new CardsFlipped([card.ObjectId], true)
        { SubjectFaces = new Dictionary<int, string> { [card.ObjectId] = "previous-public-face" } };
        VisibleResult visible = WorldProjection.For(world, null, [happened],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        Assert.Equal("previous-public-face", Assert.Single(EventPresenter.Present(visible.Events, visible.World))
            .Activity!.Subjects[0].Reference!.FaceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FaceGrantsUseTheSameViewerPolicyAsEveryOtherCardRoute(bool cooperative)
    {
        World world = VisibilityFixture.Board();
        Card card = world.Seats[1].Hand.Cards[0];
        var happened = new FieldSet(card.ObjectId, "health", 7, 6)
        { SubjectFaces = new Dictionary<int, string> { [card.ObjectId] = card.FaceId } };
        IVisibilityPolicy policy = cooperative ? new PermissiveVisibilityPolicy() : new RestrictedVisibilityPolicy(0);
        VisibleResult visible = WorldProjection.For(world, null, [happened], policy.Authorize(null, world.Players));
        Assert.Equal(cooperative ? 1 : 0, visible.World.EventFaces.Count);
        Assert.Equal(cooperative ? 1 : 0, visible.Events.Count);
    }

    [Fact]
    public void AVisibleOccurrenceCannotCarryAnUnrelatedPrivateFaceGrant()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        Card hidden = world.Seats[1].Hand.Cards[0];
        var happened = new FieldSet(villain.ObjectId, "health", 7, 6)
        { SubjectFaces = new Dictionary<int, string> { [villain.ObjectId] = villain.FaceId, [hidden.ObjectId] = hidden.FaceId } };
        VisibleResult visible = WorldProjection.For(world, null, [happened],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        Assert.Equal(villain.FaceId, Assert.Single(visible.World.EventFaces).Key);
        Assert.Equal(villain.ObjectId, Assert.Single(Assert.Single(visible.Events).SubjectFaces!).Key);
    }
}
