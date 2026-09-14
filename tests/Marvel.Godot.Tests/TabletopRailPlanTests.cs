using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TabletopRailPlanTests
{
    [Fact]
    public void PhysicalRailsKeepLiveScenarioAndExpandedPlayerAreasDistinct()
    {
        BoardAreaPresentation villain = Area(1, -1, "VillainArea", BoardAreaProminence.Live);
        BoardAreaPresentation scheme = Area(2, -1, "MainSchemesArea", BoardAreaProminence.Live);
        BoardAreaPresentation encounterDeck = Area(3, -1, "EncounterDeck", BoardAreaProminence.Supporting);
        BoardAreaPresentation other = Area(4, -1, "EnvironmentArea", BoardAreaProminence.Live);
        BoardAreaPresentation identity = Area(5, 1, "HeroArea", BoardAreaProminence.Live);
        BoardAreaPresentation deck = Area(6, 1, "PlayerDeck", BoardAreaProminence.Supporting);
        BoardAreaPresentation otherPlayer = Area(7, 0, "HeroArea", BoardAreaProminence.Live);
        TabletopRailPlan plan = TabletopRailPlan.Create(
        [
            new BoardLanePresentation("scenario", "SCENARIO", null, [villain, scheme, encounterDeck]),
            new BoardLanePresentation("player-0", "PLAYER 1", 0, [otherPlayer]),
            new BoardLanePresentation("player-1", "PLAYER 2", 1, [identity, deck]),
            new BoardLanePresentation("other", "OTHER", null, [other]),
        ], 1);

        Assert.Equal([1, 2], plan.FarLive.Select(area => area.Id));
        Assert.Equal([3, 4], plan.FarShelf.Select(area => area.Id));
        Assert.Equal([5], plan.NearLive.Select(area => area.Id));
        Assert.Equal([6], plan.NearShelf.Select(area => area.Id));

        int[] displayedAreas = [.. plan.FarLive
            .Concat(plan.FarShelf)
            .Concat(plan.NearLive)
            .Concat(plan.NearShelf)
            .Select(area => area.Id)];

        Assert.Equal(displayedAreas.Length, displayedAreas.Distinct().Count());
        Assert.DoesNotContain(otherPlayer.Id, displayedAreas);
    }

    [Fact]
    public void SeatMarkersKeepEachAuthoritativeRoleSeparate()
    {
        var selection = new DisplayedSeatSelection(
            ExpandedSeat: 3,
            ActivePlayer: 2,
            PromptOwner: 3,
            ViewedPrivateSeat: 0,
            PublicFocusSeat: 1);

        Assert.Equal("TURN", MulliganSeatStripRenderer.RoleMarkers(2, selection));
        Assert.Equal("DECISION", MulliganSeatStripRenderer.RoleMarkers(3, selection));
        Assert.Equal("PRIVATE VIEW", MulliganSeatStripRenderer.RoleMarkers(0, selection));
        Assert.Equal("PUBLIC FOCUS", MulliganSeatStripRenderer.RoleMarkers(1, selection));
        Assert.Equal("OBSERVING", MulliganSeatStripRenderer.RoleMarkers(4, selection));
    }

    [Fact]
    public void EventAnchorOnAnUnexpandedSeatStillIdentifiesItsVisibleWorkspace()
    {
        BoardAreaPresentation first = Area(8, 0, "HeroArea", BoardAreaProminence.Live) with
        {
            Cards = [Card(80)],
        };
        BoardAreaPresentation second = Area(9, 1, "HeroArea", BoardAreaProminence.Live) with
        {
            Cards = [Card(90)],
        };
        var board = new BoardPresentation([first, second]);

        Assert.Equal(1, TabletopAnchorSeat.For(board, [90]));
    }

    [Fact]
    public void ConcealedHandCountIsNotPresentedAsAnEmptyHand()
    {
        TabletopHandShelfSummary summary = TabletopHandShelfSummary.From([], concealed: 3);

        Assert.Equal("HAND  ·  0 VISIBLE  ·  3 CONCEALED", summary.Heading);
        Assert.Equal("3 concealed cards in hand.", summary.EmptyMessage);
    }

    private static BoardAreaPresentation Area(
        int id,
        int seat,
        string zone,
        BoardAreaProminence prominence) => new(id, zone, string.Empty, [], [])
    {
        Zone = zone,
        Seat = seat,
        Prominence = prominence,
    };

    private static BoardCardPresentation Card(int id) => new(
        id, 1, false, "Visible card", string.Empty, "HERO", string.Empty, []);
}
