using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.State;
public sealed class PlacesDetachingTests : PlacesTestBase
{
    [Fact]
    public void DetachingIsOneOperationAndNamesThePriorGameArea()
    {
        var world = Ordinary(players: 1);
        var prior = Assert.Single(world.GameAreas);
        var events = new List<GameEvent>();
        world.Detach(PlayArea.Of(0), "test", events);
        Assert.Null(world.GameAreaOf(PlayArea.Of(0)));
        var detached = Assert.IsType<PlayAreaDetached>(Assert.Single(events));
        Assert.Equal(0, detached.PlayArea);
        Assert.Equal(prior.Id, detached.GameArea);
        Assert.Equal("test", detached.Trigger);
        Assert.Equal("Detach", detached.Verb);
    }

    [Fact]
    public void DetachingAPlayAreaOutsideThisWorldIsSilentAndAtomic()
    {
        var world = Ordinary(players: 1);
        var prior = Assert.Single(world.GameAreas);
        var before = prior.PlayAreas.ToHashSet();
        var events = new List<GameEvent>();
        world.Detach(PlayArea.Of(99), "test", events);
        Assert.Empty(events);
        Assert.Equal(before, prior.PlayAreas.ToHashSet());
    }

    [Fact]
    public void DetachingAnAlreadyDetachedPlayAreaIsSilent()
    {
        var world = Ordinary(players: 1);
        var events = new List<GameEvent>();
        world.Detach(PlayArea.Of(0), "test", events);
        events.Clear();
        world.Detach(PlayArea.Of(0), "test", events);
        Assert.Empty(events);
        Assert.Null(world.GameAreaOf(PlayArea.Of(0)));
    }

    // ---------------------------------------------------------------- the wire
    [Fact]
    public void AnAreaReferenceCarriesThePlayAreaAndNotTheCardOwner()
    {
        // The confusion this whole model exists to end. A player's nemesis pile
        // is *theirs* -- so its play area is theirs -- and is the scenario's
        // property, so a card made in it is owned by -1. The wire type carries
        // the first. Picking the second would be invisible in an ordinary game
        // and wrong on exactly the cards where whose-is-it drives rules.
        var world = Ordinary(players: 2);
        var nemesis = world.Seats[1].Nemesis;
        Assert.Equal(World.Scenario, nemesis.CardOwner);
        Assert.Equal(PlayArea.Of(1), nemesis.PlayArea);
        var reference = Places.Reference(nemesis);
        Assert.Equal(1, reference.Owner);
        Assert.Equal("AsideDeck", reference.Zone);
        Assert.True(reference.IsIdentified);
    }

    [Fact]
    public void TheVillainsPlayAreaIsMinusOneOnTheWire()
    {
        var world = Ordinary(players: 1);
        var encounter = world.CreateArea(DeckType.EncounterDeck);
        Assert.Equal(PlayArea.Villains, encounter.PlayArea);
        Assert.Equal(-1, Places.Reference(encounter).Owner);
    }

    // ------------------------------------------------------------- composition
    [Fact]
    public void TheTwoPartitionsCompose()
    {
        // Nothing published needs both at once, but both say what a card cannot
        // reach, so the answer is the intersection rather than one overriding
        // the other. Asserted because getting it wrong is invisible until a
        // scenario does need both, and then it is a rules bug rather than a
        // crash.
        var world = Ordinary(players: 2);
        var mine = MainScheme(world, PlayArea.Of(0));
        MainScheme(world, PlayArea.Of(1));
        Split(world);
        // A side scheme in the villain's play area would see both main schemes
        // by the Fear No Evil rule -- but it is in the villain's game area, and
        // both main schemes are in the players'.
        var sideScheme = InPlayArea(world, DeckType.SideSchemesArea, PlayArea.Villains);
        Assert.Empty(Places.MainSchemes(world, sideScheme));
        // An ally beside the main scheme still sees it.
        var myAlly = InPlayArea(world, DeckType.AlliesArea, PlayArea.Of(0));
        Assert.Equal([mine], Places.MainSchemes(world, myAlly));
    }
}
