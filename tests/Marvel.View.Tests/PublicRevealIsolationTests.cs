using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PublicRevealIsolationTests
{
    [Fact]
    public void HistoricalRevealUsesItsCapturedFaceTextAfterLaterFormChange()
    {
        // Synthetic disclosure contract: this does not claim an Invocation can change form.
        var (world, card, _) = Create();
        CardsFlipped reveal = Reveal(card);
        card.TurnTo("01001a");
        var visible = WorldProjection.For(world, null, [reveal], ViewScope.None);
        var captured = visible.World.EventFaces["01001b"];
        string laterText = Assert.Single(visible.World.Areas.SelectMany(area => area.Cards),
            entry => entry.Id == card.ObjectId).Face!.RulesText;
        Assert.NotEqual(captured.RulesText, laterText);
        string summary = Assert.Single(EventPresenter.Present(visible.Events, visible.World)).Summary;
        Assert.Contains(captured.Title, summary);
        Assert.Contains(captured.RulesText, summary);
        Assert.DoesNotContain(laterText, summary);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void OneOccurrenceDoesNotAuthorizeOtherEventsOrAnotherIncarnation(int viewer)
    {
        // Synthetic event stream isolates the lifetime of a public grant across departure/reentry.
        var (world, card, other) = Create();
        CardsFlipped reveal = Reveal(card);
        int incarnation = card.Incarnation;
        World.MoveToTop(card, world.Seats[0].Hand);
        World.MoveToTop(card, world.Seats[0].Hero);
        Assert.True(card.Incarnation > incarnation);
        card.TurnTo("01001a");
        World.MoveToTop(card, world.Seats[0].Deck);
        CardsFlipped later = new([card.ObjectId, other.ObjectId], true)
        { SubjectFaces = new Dictionary<int, string> { [card.ObjectId] = card.FaceId, [other.ObjectId] = other.FaceId } };
        CardsMoved moved = new(Places.Reference(world.Seats[0].Hand), Places.Reference(card.Area),
            [new Landing(card.ObjectId, 0)]);
        AreaReordered reordered = new(Places.Reference(card.Area), [card.ObjectId, other.ObjectId]);
        ViewScope scope = viewer < 0 ? ViewScope.None : new RestrictedVisibilityPolicy(viewer).Authorize(null, 2);
        var visible = WorldProjection.For(world, null, [reveal, later, moved, reordered], scope);
        CardsFlipped retained = Assert.IsType<CardsFlipped>(Assert.Single(visible.Events));
        Assert.Equal([card.ObjectId], retained.Cards);
        Assert.Equal("01001b", retained.SubjectFaces![card.ObjectId]);
        Assert.Null(retained.PublicFaces);
        Assert.Equal(["01001b"], visible.World.EventFaces.Keys);
        Assert.DoesNotContain(visible.World.Areas.SelectMany(area => area.Cards),
            entry => entry.Id == card.ObjectId || entry.Id == other.ObjectId);
        var facts = new ActionHistoryFacts(0, "Player", "action", "PlayerTurn", Game.ActionVerb,
            "Synthetic reveal", null, [], []);
        string detail = Assert.Single(ActionHistoryPresenter.PresentEntry(facts,
            [reveal, later, moved, reordered], visible.World).Details);
        Assert.Contains(world.Facts.Title("01001b"), detail);
        Assert.DoesNotContain(world.Facts.Title("01001a"), detail);
    }

    private static CardsFlipped Reveal(Card card) => new([card.ObjectId], true)
    {
        SubjectFaces = new Dictionary<int, string> { [card.ObjectId] = card.FaceId },
        PublicFaces = new Dictionary<int, string> { [card.ObjectId] = card.FaceId },
    };

    private static (World World, Card Revealed, Card Other) Create()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 5489);
        var owner = world.CreateSeat("Owner");
        var other = world.CreateSeat("Other");
        Card card = world.CreateCard("01001b,01001a", owner.Hero);
        Card hidden = world.CreateCard("01088", other.Deck);
        return (world, card, hidden);
    }
}
