using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class SplitTableProjectionTests
{
    private static readonly CardCatalog Facts =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void AnOrdinaryTableHasOneCentralAreaNoReachLimitsAndNoKangFacts()
    {
        var world = new World(Facts, 2, 42);
        for (int seat = 0; seat < 2; seat++)
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097a,01097b", world.AreaOf(DeckType.MainSchemesArea));

        var result = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));

        var area = Assert.Single(result.World.GameAreas);
        Assert.True(area.Central);
        Assert.False(area.Retained);
        Assert.Equal([villain.ObjectId], area.Villains);
        Assert.Equal([scheme.ObjectId], area.MainSchemes);
        Assert.Equal("the game area (Rhino)", area.Label);
        Assert.All(result.World.Players, player => Assert.Empty(player.OutOfReach));
        Assert.Null(result.World.Kang);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EachSeatSeesEveryAreasSummaryAndWhatItsCardsCannotReach(int viewer)
    {
        // pack:mc11:game-areas: "Cards and components in one game area cannot affect another
        // game area (with the exception of the text on stage 2B)."
        var (world, kangs, schemes, central) = Separated();
        var hidden = world.CreateCard("01088", world.Seats[1 - viewer].Hand);

        var result = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(viewer).Authorize(null, 2));

        for (int seat = 0; seat < 2; seat++)
        {
            var area = Assert.Single(result.World.GameAreas, item => item.PlayAreas.SequenceEqual([seat]));
            Assert.Equal([kangs[seat].ObjectId], area.Villains);
            Assert.Equal([schemes[seat].ObjectId], area.MainSchemes);
            Assert.False(area.Central);
            Assert.Equal($"p{seat}'s game area ({EffectiveCards.Title(kangs[seat], Facts)})", area.Label);
            var player = result.World.Players[seat];
            Assert.Equal(new[] { world.Seats[1 - seat].IdentityCard.ObjectId, kangs[1 - seat].ObjectId,
                schemes[1 - seat].ObjectId }.Order(), player.OutOfReach);
            Assert.DoesNotContain(central.ObjectId, player.OutOfReach);
            Assert.DoesNotContain(hidden.ObjectId, player.OutOfReach);
        }
        var kang = Assert.IsType<KangTableDescriptor>(result.World.Kang);
        Assert.Equal((2, 2, 0), (kang.Stage, kang.RemainingLocalKangs, kang.StoredDominions));
        Assert.Equal([central.ObjectId], kang.CentralSchemes);
        Assert.Equal(2, kang.StartingPlayers);
        Assert.Equal(world.AreaOf(DeckType.EncounterDeck).Id, kang.EncounterDeck);
        Assert.Equal(world.AreaOf(DeckType.EncounterDiscardPile).Id, kang.EncounterDiscard);
        Assert.Equal(
            [KangNoteKind.AreaIsolation, KangNoteKind.StepsInTurnOrder, KangNoteKind.EachPlayerIsArea,
                KangNoteKind.SharedEncounterDeck, KangNoteKind.CentralTokensWaitFor4A,
                KangNoteKind.CompletionStoresDominion, KangNoteKind.CompletionStoresDominion],
            kang.Notes.Select(note => note.Kind));
        Assert.Equal(central.ObjectId, Assert.Single(kang.Notes, note => note.Kind == KangNoteKind.CentralTokensWaitFor4A).Card);
        Assert.Equal(schemes.Select(scheme => (int?)scheme.ObjectId), kang.Notes
            .Where(note => note.Kind == KangNoteKind.CompletionStoresDominion).Select(note => note.Card));
        Assert.Contains("counts all 2 players who started",
            Assert.Single(kang.Notes, note => note.Kind == KangNoteKind.EachPlayerIsArea).Text, StringComparison.Ordinal);
        Assert.All(kang.Notes, note => Assert.False(string.IsNullOrWhiteSpace(note.Text)));
        // The original, emptied game area is not listed.
        Assert.Equal(2, result.World.GameAreas.Count);
        Assert.StartsWith("2 Kang (II) remain.", kang.Progress, StringComparison.Ordinal);
        Assert.Empty(kang.Departures);
        Assert.Null(kang.Acting);
    }

    [Fact]
    public void ReachListsOnlyCardsInPlayEvenWhenEveryHandIsVisible()
    {
        var (world, kangs, schemes, _) = Separated();
        var hand = world.CreateCard("01088", world.Seats[1].Hand);

        var result = WorldProjection.For(world, null, [], new PermissiveVisibilityPolicy().Authorize(null, 2));

        Assert.DoesNotContain(hand.ObjectId, result.World.Players[0].OutOfReach);
        Assert.Contains(kangs[1].ObjectId, result.World.Players[0].OutOfReach);
        Assert.Contains(schemes[1].ObjectId, result.World.Players[0].OutOfReach);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void APendingDepartureAndTheActingAreaReachEverySeat(int viewer)
    {
        // 11002-11005: "At the end of the phase, remove Kang from the game and join another
        // game area." Ruling 97d62cdb9ce436a7: each Kang activates "in their game area in turn order".
        var (world, kangs, _, _) = Separated();
        world.Agenda.KangTransitions.CaptureJoin(world, kangs[0], 1, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Index: 1, Subject: kangs[1].ObjectId, Seat: 1));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));

        var result = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(viewer).Authorize(null, 2));

        var kang = Assert.IsType<KangTableDescriptor>(result.World.Kang);
        Assert.Equal(1, kang.RemainingLocalKangs);
        var departure = Assert.Single(kang.Departures);
        int destination = world.GameAreaOf(PlayArea.Of(1))!.Id;
        Assert.Equal((kangs[0].ObjectId, world.GameAreaOf(PlayArea.Of(0))!.Id), (departure.Villain, departure.SourceGameArea));
        Assert.Equal([0], departure.Seats);
        Assert.Equal([destination], departure.Destinations);
        Assert.False(departure.Reunion);
        Assert.Null(departure.CompletedScheme);
        Assert.Contains("so p0 leaves p0's game area", departure.Summary, StringComparison.Ordinal);
        var acting = Assert.IsType<VillainPhaseFocusDescriptor>(kang.Acting);
        Assert.Equal((2, 1, (int?)destination), (acting.Step, acting.Seat, acting.GameArea));
        Assert.Equal("Villain phase step 2: the villain and engaged minions activate for p1, in p1's game area "
            + $"({EffectiveCards.Title(kangs[1], Facts)}).", acting.Summary);
    }

    [Fact]
    public void BeforeTheSplitProgressNamesKangIAndThenHisPendingSplit()
    {
        // 11001: Kang (I)'s defeat splits the table at the end of the phase.
        var world = new World(Facts, 1, 42);
        world.CreateSeat("p0").IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        var kang = world.CreateCard("11001", world.AreaOf(DeckType.VillainArea));
        var scope = new PermissiveVisibilityPolicy().Authorize(null, 1);

        var before = Assert.IsType<KangTableDescriptor>(WorldProjection.For(world, null, [], scope).World.Kang);
        world.Agenda.KangSplit.Capture(world, kang);
        var after = Assert.IsType<KangTableDescriptor>(WorldProjection.For(world, null, [], scope).World.Kang);

        Assert.Equal((1, (int?)null), (before.Stage, before.PendingSplit));
        Assert.Equal("Defeat Kang (I) to split the table into separate game areas.", before.Progress);
        Assert.Empty(before.Notes);
        Assert.Equal((1, (int?)kang.ObjectId), (after.Stage, after.PendingSplit));
        Assert.StartsWith("Kang (I) is defeated.", after.Progress, StringComparison.Ordinal);
    }

    [Fact]
    public void ASolePlayerReadsThatPerPlayerValuesCountOnePlayer()
    {
        // rr:per-player-icon: "multiplies that value by the number of players who started the scenario".
        var world = new World(Facts, 1, 42);
        world.CreateSeat("p0").IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        world.Join(PlayArea.Of(0), world.CreateGameArea(), "prepared split", []);
        var local = world.CreateCard("11002", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        world.Detach(PlayArea.Villains, "prepared split", []);
        var scope = new PermissiveVisibilityPolicy().Authorize(null, 1);

        var kang = WorldProjection.For(world, null, [], scope).World.Kang!;

        Assert.EndsWith("counts the 1 player who started the scenario.",
            Assert.Single(kang.Notes, note => note.Kind == KangNoteKind.EachPlayerIsArea).Text, StringComparison.Ordinal);
        Assert.StartsWith("1 Kang (II) remains.", kang.Progress, StringComparison.Ordinal);

        world.Agenda.KangTransitions.CaptureJoin(world, local, 1, PhaseTransitionKind.EndPlayerPhase);
        var last = WorldProjection.For(world, null, [], scope).World.Kang!;
        Assert.True(Assert.Single(last.Departures).Reunion);
        Assert.StartsWith("No Kang (II) remains. At the end of the phase the last group leaves", last.Progress,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TopologyEventsNameTheSeatsTheyHadWhenTheyHappened()
    {
        // Two groups joining one area in the same response: each event names the area as it
        // stood at its own occurrence, never the final snapshot's membership.
        var world = new World(Facts, 3, 42);
        for (int seat = 0; seat < 3; seat++)
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
        var result = WorldProjection.For(world, null, [], new PermissiveVisibilityPolicy().Authorize(null, 3));

        string Present(GameEvent happened) => EventPresenter.Present(happened, result.World).Summary;

        Assert.Equal("p0's play area joined p2's game area.", Present(new PlayAreaJoined(0, 5) { Members = [0, 2] }));
        Assert.Equal("p1's play area joined p0 and p2's game area.", Present(new PlayAreaJoined(1, 5) { Members = [0, 1, 2] }));
        Assert.Equal("p0's play area formed its own game area.", Present(new PlayAreaJoined(0, 6) { Members = [0] }));
        Assert.Equal("p0's play area joined the villain's game area.", Present(new PlayAreaJoined(0, 0) { Members = [-1, 0, 1] }));
        Assert.Equal("The villain play area joined p0 and p1's game area.", Present(new PlayAreaJoined(-1, 4) { Members = [-1, 0, 1] }));
        Assert.Equal("p0's play area joined a game area.", Present(new PlayAreaJoined(0, 99)));
        Assert.Equal("p0's play area left p1's game area.", Present(new PlayAreaDetached(0, 4) { Members = [1] }));
        Assert.Equal("p0's play area left its game area.", Present(new PlayAreaDetached(0, 4) { Members = [] }));
        Assert.Equal("p0's play area left its game area.", Present(new PlayAreaDetached(0, 4)));
        Assert.Equal("The villain play area left the shared game area; the players are now in separate game areas.",
            Present(new PlayAreaDetached(-1, 0) { Members = [0, 1, 2] }));
    }

    [Fact]
    public void TheBoardPresentationCarriesTheSplitFactsAndEachCardsGameArea()
    {
        var (world, kangs, schemes, central) = Separated();
        var visible = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2)).World;

        var board = BoardPresentation.From(visible);

        Assert.Equal(visible.GameAreas, board.GameAreas);
        Assert.Same(visible.Kang, board.Kang);
        Assert.Equal(visible.Players, board.Players);
        var cards = board.Areas.SelectMany(area => area.Cards).Where(card => card.TargetId is not null)
            .ToDictionary(card => card.TargetId!.Value);
        for (int seat = 0; seat < 2; seat++)
        {
            int? area = world.GameAreaOf(PlayArea.Of(seat))!.Id;
            Assert.Equal(area, cards[kangs[seat].ObjectId].GameArea);
            Assert.Equal(area, cards[schemes[seat].ObjectId].GameArea);
        }
        Assert.Null(cards[central.ObjectId].GameArea);
    }

    private static (World World, Card[] Kangs, Card[] Schemes, Card Central) Separated()
    {
        var world = new World(Facts, 2, 42);
        var kangs = new Card[2];
        var schemes = new Card[2];
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
            world.Join(PlayArea.Of(seat), world.CreateGameArea(), "prepared split", []);
            kangs[seat] = world.CreateCard(seat == 0 ? "11002" : "11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(seat)));
            schemes[seat] = world.CreateCard(seat == 0 ? "11009a,11009b" : "11010a,11010b",
                world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(seat)));
            schemes[seat].TurnTo(seat == 0 ? "11009b" : "11010b");
        }
        var central = world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesArea));
        world.Detach(PlayArea.Villains, "prepared split", []);
        return (world, kangs, schemes, central);
    }
}
