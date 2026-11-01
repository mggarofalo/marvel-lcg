using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The engine's read-only game-area summaries and Kang progress facts, on the
// nonpublic actual-card Kang boards. Kang's public admission stays closed.
public sealed class KangTableFactsTests : CoreCardTestBase
{
    [Fact]
    public void AGameWithoutKangHasNoKangFacts()
    {
        var world = new World(Cards, 1, 1);
        world.CreateSeat("Spider-Man").IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));

        Assert.Null(KangTable.Of(world));
        var only = Assert.Single(GameAreaFacts.Of(world));
        Assert.True(only.Central);
        Assert.Equal("the game area (Rhino)", only.Label);
    }

    [Fact]
    [Rule("rr:winning-the-game")]
    public void BeforeTheSplitKangIStandsInTheCentralArea()
    {
        // 11001: Kang (I) is the first villain stage; his defeat splits the table at
        // the end of the phase rather than winning.
        var world = KangPlayerCountFixture.Board(2, false);
        var kang = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);

        var area = Assert.Single(GameAreaFacts.Of(world));
        Assert.True(area.Central);
        Assert.False(area.Retained);
        Assert.Equal([kang.ObjectId], area.Villains);
        Assert.Equal([scheme.ObjectId], area.MainSchemes);
        Assert.Equal("the game area (Kang (The Conqueror))", area.Label);
        var facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        Assert.Equal((1, 0, 0, null, 0), (facts.Stage, facts.RemainingLocalKangs, facts.StoredDominions, facts.PendingSplit,
            facts.LocalKangsDealt));
        Assert.Empty(facts.CentralSchemes);
        Assert.Empty(facts.Departures);
        Assert.Empty(facts.Notes);

        KangPlayerCountFixture.Defeat(world, kang, []);

        facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        Assert.Equal(1, facts.Stage);
        Assert.Equal(kang.ObjectId, facts.PendingSplit);
    }

    [Fact]
    public void TheSplitNamesEachAreasLocalKangAndStageBesideCentral2B()
    {
        // pack:mc11:areas: each stage 3A goes "directly in front of your play area" in
        // your own game area, with its Kang (II); 2B "remains in play in a central
        // location [...] though it is not part of any other game area".
        var (world, _) = Split(2, completion: false);
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);

        var facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        Assert.Equal((2, 2, 2), (facts.Stage, facts.RemainingLocalKangs, facts.LocalKangsDealt));
        Assert.Equal([central.ObjectId], facts.CentralSchemes);
        for (int seat = 0; seat < 2; seat++)
        {
            var area = Summary(world, seat);
            var kang = Local(world, seat, DeckType.VillainArea);
            Assert.Equal([kang.ObjectId], area.Villains);
            Assert.Equal([Local(world, seat, DeckType.MainSchemesArea).ObjectId], area.MainSchemes);
            Assert.False(area.Central);
            Assert.False(area.Retained);
            Assert.Empty(area.FormerSeats);
            Assert.Equal($"{world.Seats[seat].Name}'s game area ({Title(world, kang)})", area.Label);
        }
        // Kang (I)'s emptied area holds nothing that can return to it, so it is not listed.
        Assert.Equal(2, GameAreaFacts.Of(world).Count);
        Assert.Equal(
            [(KangNoteKind.AreaIsolation, (int?)null), (KangNoteKind.StepsInTurnOrder, null),
                (KangNoteKind.EachPlayerIsArea, null), (KangNoteKind.SharedEncounterDeck, null),
                (KangNoteKind.CentralTokensWaitFor4A, central.ObjectId),
                .. Enumerable.Range(0, 2).Select(seat => Local(world, seat, DeckType.MainSchemesArea).ObjectId).Order()
                    .Select(id => (KangNoteKind.CompletionStoresDominion, (int?)id))],
            facts.Notes.Select(note => (note.Kind, note.Card)));
    }

    [Fact]
    [Rule("rr:first-player.3")]
    public void ADefeatedLocalKangsGroupWaitsToJoinTheAreaItCanReach()
    {
        // 11002-11005: "When Defeated: Remove [stage 3B] from the game. At the end of the
        // phase, remove Kang from the game and join another game area."
        var (world, events) = Split(2, completion: false);
        var defeated = Local(world, 0, DeckType.VillainArea);
        string destination = Summary(world, 1).Label;
        KangPlayerCountFixture.Defeat(world, defeated, events);
        // pack:mc11:rules-clarifications: the defeated Kang (II) leaves play at once, so
        // the area he leaves is named by its players alone.
        Assert.Equal(DeckType.RemovedArea, defeated.Area.Type);
        string source = Summary(world, 0).Label;
        Assert.Equal("Spider-Man's game area", source);

        var facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        var departure = Assert.Single(facts.Departures);
        Assert.Equal(1, facts.RemainingLocalKangs);
        Assert.Equal(defeated.ObjectId, departure.Villain);
        Assert.Equal([0], departure.Seats);
        Assert.Null(departure.CompletedScheme);
        Assert.Equal([world.GameAreaOf(PlayArea.Of(1))!.Id], departure.Destinations);
        Assert.False(departure.Reunion);
        string sentence = Sentence(world);
        Assert.Equal($"{Title(world, defeated)} was defeated, so Spider-Man leaves {source} at the end of the phase.", sentence);

        KangPlayerCountFixture.Boundary(world, 2, events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var offer = Assert.Single(join.Affordances);
        Assert.Equal($"Join {destination}", offer.DisplayLabel);
        Assert.Equal("Move Spider-Man into this game area after unique copies are reconciled.", offer.Description);
        Assert.StartsWith(sentence, join.Description, StringComparison.Ordinal);
        Assert.Equal($"game area {departure.Destinations[0]}", offer.Label);
        int before = events.Count;
        Sequence.Answer(world, Cards, world.Abilities, join, Decision.Take(offer.Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        // The join records the destination's membership when it happens.
        var joined = Assert.Single(events.Skip(before).OfType<PlayAreaJoined>());
        Assert.Equal((0, departure.Destinations[0]), (joined.PlayArea, joined.GameArea));
        Assert.Equal([0, 1], joined.Members);

        // The resolved departure leaves the split's count as it was dealt.
        Assert.Equal((1, 2), (KangTable.Of(world)!.RemainingLocalKangs, KangTable.Of(world)!.LocalKangsDealt));
        var last = Local(world, 1, DeckType.VillainArea);
        KangPlayerCountFixture.Defeat(world, last, events);
        facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        Assert.Equal((2, 0, 2), (facts.Stage, facts.RemainingLocalKangs, facts.LocalKangsDealt));
        departure = Assert.Single(facts.Departures);
        Assert.Equal([0, 1], departure.Seats);
        Assert.Empty(departure.Destinations);
        Assert.True(departure.Reunion);
        Assert.Equal($"{Title(world, last)} was defeated, so Spider-Man and Black Widow leave Spider-Man and Black "
            + "Widow's game area at the end of the phase.", Sentence(world));
    }

    [Fact]
    public void ACompletedStageStoresADominionAndMovesItsGroupWithoutLosing()
    {
        // 11009b-11012b: "When this stage is completed, place 1 set-aside Kang's Dominion
        // facedown under stage 4A." pack:mc11:game-areas: "It is better to let Kang (II)
        // complete his main scheme than to let Kang (II) defeat your hero."
        var (world, events) = Split(2, completion: true);
        var scheme = Local(world, 0, DeckType.MainSchemesArea);
        var kang = Local(world, 0, DeckType.VillainArea);
        Agendas.Happening(world);
        Threat.Schedule(world, new ThreatBatch([scheme], null, 99, ThreatCause.CardAbility, "test", 0));
        Sequence.Finish(world, Cards, world.Abilities, events);

        Assert.Equal(Outcome.Unfinished, world.Result);
        var facts = Assert.IsType<KangTableFacts>(KangTable.Of(world));
        Assert.Equal(1, facts.StoredDominions);
        Assert.Equal(1, facts.RemainingLocalKangs);
        var departure = Assert.Single(facts.Departures);
        Assert.Equal((kang.ObjectId, scheme.ObjectId), (departure.Villain, departure.CompletedScheme));
        // The completed stage has stored its Dominion; only the other stage still carries the note.
        Assert.Equal([Local(world, 1, DeckType.MainSchemesArea).ObjectId], facts.Notes
            .Where(note => note.Kind == KangNoteKind.CompletionStoresDominion).Select(note => note.Card!.Value));
        Assert.Equal($"{Title(world, kang)}'s {Title(world, scheme)} was completed, storing a Kang's Dominion under "
            + $"stage 4A, so Spider-Man leaves {Summary(world, 0).Label} at the end of the phase.", Sentence(world));
    }

    [Fact]
    [Rule("rr:player-elimination.step.5")]
    public void AnEliminatedPlayersAreaIsRetainedUntilItsDeparture()
    {
        // 11009b-11012b: "If all the players at this stage are defeated, this stage is
        // completed." Its Kang and stage stay until the printed phase-end removal.
        var (world, events) = Split(2, completion: true);
        var kang = Local(world, 0, DeckType.VillainArea);
        var source = world.GameAreaOf(PlayArea.Of(0))!;
        Agendas.Happening(world);
        Defeat.Character(world, Cards, world.Seats[0].IdentityCard, "identity defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);

        var area = Assert.Single(GameAreaFacts.Of(world), item => ReferenceEquals(item.Area, source));
        Assert.True(area.Retained);
        Assert.Empty(area.Area.PlayAreas);
        Assert.Equal([0], area.FormerSeats);
        Assert.Equal([kang.ObjectId], area.Villains);
        Assert.Equal($"Spider-Man's retained game area ({Title(world, kang)})", area.Label);
        Assert.False(Summary(world, 1).Retained);
        var departure = Assert.Single(Assert.IsType<KangTableFacts>(KangTable.Of(world)).Departures);
        Assert.Empty(departure.Seats);
        Assert.NotNull(departure.CompletedScheme);
        Assert.Contains("so Spider-Man's retained components leave", Sentence(world), StringComparison.Ordinal);
    }

    [Fact]
    [Rule("rr:first-player.3")]
    public void TheDepartureOrderNamesEachGroupAndItsKang()
    {
        var (world, events) = Split(3, completion: false);
        Card[] kangs = [Local(world, 0, DeckType.VillainArea), Local(world, 1, DeckType.VillainArea)];
        KangPlayerCountFixture.Defeat(world, kangs[0], events);
        KangPlayerCountFixture.Defeat(world, kangs[1], events);
        string[] labels = [.. Enumerable.Range(0, 2).Select(seat => $"Resolve {Title(world, kangs[seat])}'s departure from {Summary(world, seat).Label}")];
        KangPlayerCountFixture.Boundary(world, 2, events);

        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));

        Assert.Equal(KangDepartureOrderOperation.Order, prompt.Trigger);
        Assert.Equal(labels.Order(),
            prompt.Affordances.Select(offer => offer.DisplayLabel).Order());
        Assert.Equal(["Resolve Black Widow next", "Resolve Spider-Man next"],
            prompt.Affordances.Select(offer => offer.CommitLabel).Order());
    }

    [Fact]
    [Rule("rr:villain-phase.step.2")]
    public void TheActingAreaFollowsEachSeatsActivationInTurnOrder()
    {
        // Ruling 97d62cdb9ce436a7: "each player should resolve the activation of the Kang in
        // their game area in turn order".
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, false, events);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        world.Seats[1].IdentityCard.TurnTo("08001a");
        Assert.Null(VillainPhaseFocus.Of(world));
        int[] order = [.. world.PlayerOrder];
        Marvel.Rules.Play.VillainPhase.Schedule(world.Agenda, 1);
        Assert.Null(VillainPhaseFocus.Of(world));

        var acting = new List<int>();
        var prompt = Sequence.Work(world, Cards, world.Abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 80, prompt.Description);
            if (VillainPhaseFocus.Of(world) is { Step: 2 } focus)
            {
                Assert.Equal(prompt.Player, focus.Seat);
                Assert.Equal(world.GameAreaOf(PlayArea.Of(focus.Seat))!.Id, focus.GameArea);
                if (acting.Count == 0 || acting[^1] != focus.Seat) acting.Add(focus.Seat);
            }
            var decision = prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, Cards, world.Abilities, prompt, decision, events);
            prompt = Sequence.Work(world, Cards, world.Abilities, events);
        }

        Assert.Equal(order, acting);
        Assert.Null(VillainPhaseFocus.Of(world));
    }

    // The engine's sentence for the one pending departure, which its boundary prompts open with.
    private static string Sentence(World world) =>
        KangDepartureNames.Departure(world, Assert.Single(world.Agenda.KangTransitions.Pending));

    private static (World World, List<GameEvent> Events) Split(int players, bool completion)
    {
        var world = KangPlayerCountFixture.Board(players, false, completion);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.True(Places.Separated(world));
        return (world, events);
    }

    private static GameAreaSummary Summary(World world, int seat) =>
        Assert.Single(GameAreaFacts.Of(world), item => item.Area.Contains(PlayArea.Of(seat)));

    private static Card Local(World world, int seat, DeckType type) =>
        Assert.Single(world.AreaOf(type, PlayArea.Of(seat)).Cards);

    private static string Title(World world, Card card) => EffectiveCards.Title(card, world.Facts);
}
