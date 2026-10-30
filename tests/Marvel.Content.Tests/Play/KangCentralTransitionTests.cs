using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic actual-card path. The blueprint supplies physical nemesis associations.
public sealed class KangCentralTransitionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, "aside", "aside", 0)]
    [InlineData(true, "deck", "discard", 1)]
    [InlineData(false, "discard", "deck", 1)]
    [InlineData(true, "play", "aside", 0)]
    [Rule("rr:when-revealed-abilities.2")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:winning-the-game.1")]
    public void LastDefeatedLocalGroupReturnsToCentralAndPutsNemesesIntoPlay(bool expert, string firstZone, string secondZone, int firstPlayer)
    {
        // "If an encounter card with a When Revealed ability is put into play without being revealed,
        // the When Revealed ability does not trigger."
        // 11013b searches for each nemesis minion and puts it into play engaged.
        var world = Board(expert);
        world.FirstPlayer = firstPlayer;
        var events = new List<GameEvent>();
        var stageOne = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, stageOne, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        End(world, 1, events);
        var first = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)).Cards);
        var last = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)).Cards);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, first, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 2, 4));
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, choice, Decision.Take(Assert.Single(choice.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Same(world.GameAreaOf(PlayArea.Of(0)), world.GameAreaOf(PlayArea.Of(1)));
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        var central = Assert.Single(world.Cards, card => card.FaceId == "11008b");
        central.PlaceTokens(EncounterDeck.AccelerationToken, 2);
        long acceleration = central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        PutNemesis(world, 0, firstZone);
        PutNemesis(world, 1, secondZone);
        int beforeReunion = events.Count;
        Agendas.Happening(world);
        Defeat.Character(world, Cards, last, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        End(world, 3, events);
        Assert.Same(world.GameAreaOf(PlayArea.Villains), world.GameAreaOf(PlayArea.Of(0)));
        var scheme = Assert.Single(world.Cards, card => card.FaceId == "11013b");
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(acceleration, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        var villain = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Assert.Equal(expert ? "11039" : "11006", villain.FaceId);
        Assert.True(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(expert ? 50 : 40, Cards.PrintedValue(villain.FaceId, "HP", world.Players));
        foreach (var seat in world.Seats)
        {
            var minion = Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
            Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
            Assert.Equal(PlayArea.Of(seat.Index), minion.Area.PlayArea);
        }
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Empty(world.Agenda.Outstanding);
        int villainMove = events.FindIndex(beforeReunion, item => item is CardsMoved moved
            && moved.Cards.Any(landing => landing.Card == villain.ObjectId));
        int flip = events.FindIndex(beforeReunion, item => item is CardsFlipped flipped
            && flipped.Cards.Contains(scheme.ObjectId));
        int shuffle = events.FindIndex(beforeReunion, item => item is AreaReordered { Verb: "Shuffle", Trigger: "11013b" });
        Assert.True(villainMove >= beforeReunion && flip > villainMove && shuffle > flip);
        foreach (var seat in world.Seats)
        {
            var minion = Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
            var moves = events.OfType<CardsMoved>().Where(move => move.Verb == "Put_Into_Play"
                && move.Cards.Any(landing => landing.Card == minion.ObjectId)).ToArray();
            Assert.Equal(seat.Index == 0 && firstZone == "play" ? 0 : 1, moves.Length);
        }
        AssertFinalVictory(world, villain, events);
    }

    [Fact]
    public void VillainRevealChoiceCompletesBeforeFourBAndNextPhaseWork()
    {
        // Synthetic villain choice probes suspension inside actual 4A; it is not printed Kang III text.
        var (world, events) = ReadyForLastBoundary(revealChoice: true);
        var identity = world.Seats[0].IdentityCard;
        identity.Exhaust();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 3, 5));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var scheme = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        Assert.DoesNotContain(events.OfType<CardsFlipped>(), flip => flip.Cards.Contains(scheme.ObjectId));
        Assert.DoesNotContain(events.OfType<FieldSet>(), field => field.Card == identity.ObjectId && field.Verb == "Ready");
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal("11013b", scheme.FaceId);
        int shuffle = events.FindIndex(item => item is AreaReordered { Verb: "Shuffle", Trigger: "11013b" });
        int ready = events.FindIndex(item => item is FieldSet field && field.Card == identity.ObjectId && field.Verb == "Ready");
        Assert.True(shuffle >= 0 && ready > shuffle);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void IndependentReconstructionsMatchCentralEventsTopologyAndRandomState()
    {
        static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
        {
            Digest = world.Digest().Canonical(),
            Areas = world.GameAreas.Select(area => new { area.Id, Players = area.PlayAreas.OrderBy(play => play.Player).Select(play => play.Player) }),
            Pending = world.Agenda.KangTransitions.Pending.Select(item => new { item.Defeated.Card, item.Defeated.Incarnation, item.Source.Id, item.NextPhase }),
            Agenda = world.Agenda.Outstanding.Select(step => new { step.What, step.Round, step.Number, step.Subject, step.Seat }),
            Random = world.Random.Generator.GetState(),
            world.Random.Generator.WordsConsumed,
            Events = events.Select(item => JsonSerializer.Serialize(item, item.GetType())).ToArray(),
        });
        var (first, firstEvents) = ReadyForLastBoundary();
        var (second, secondEvents) = ReadyForLastBoundary();
        Assert.Equal(Snapshot(first, firstEvents), Snapshot(second, secondEvents));
        End(first, 3, firstEvents);
        End(second, 3, secondEvents);
        Assert.Equal(Snapshot(first, firstEvents), Snapshot(second, secondEvents));
        Assert.Empty(first.Agenda.Outstanding);
        Assert.Empty(first.Agenda.KangTransitions.Pending);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("missing_villain")]
    public void UnsupportedCentralCompositionFailsBeforePhaseMutation(string condition)
    {
        var (world, events) = ReadyForLastBoundary();
        var stage = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        if (condition == "missing_villain")
        {
            var villain = Assert.Single(world.Cards, card => card.FaceId == "11006");
            World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        }
        else
        {
            // Prepare an intentionally unsupported hosted state for the fail-closed guard.
            var aside = stage.Area;
            World.MoveToTop(stage, world.AreaOf(DeckType.MainSchemesArea));
            var hosted = world.AreaOf(DeckType.BeneathCardsPile, PlayArea.Villains, stage.ObjectId);
            world.CreateCard(condition == "dominion" ? "11023" : "01101", hosted);
            World.MoveToTop(stage, aside);
        }
        string before = world.Digest().Canonical();
        string random = JsonSerializer.Serialize(world.Random.Generator.GetState());
        int count = events.Count;
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Contains("central reunion", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(count, events.Count);
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    internal static (World World, List<GameEvent> Events) ReadyForLastBoundary(bool revealChoice = false, bool captain = false)
    {
        var world = Board(false, revealChoice, captain);
        var events = new List<GameEvent>();
        var first = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, first, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        End(world, 1, events);
        var local = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)).Cards);
        var last = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)).Cards);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, local, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 2, 4));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, last, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        return (world, events);
    }

    private static void AssertFinalVictory(World world, Card villain, List<GameEvent> events)
    {
        // 11006/11039: "When Defeated: The players win the game."
        // rr:when-defeated-abilities.2.1: "A defeated card leaves play after its
        // When Defeated ability is resolved"; rr:winning-the-game.1 permits alternate wins.
        var identity = world.Seats[0].IdentityCard;
        identity.Exhaust();
        int beforeDefeat = events.Count;
        Agendas.Happening(world);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 4, 5));
        Defeat.Character(world, Cards, villain, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.PlayersWin, world.Result);
        Assert.Equal(DeckType.VillainArea, villain.Area.Type);
        Assert.DoesNotContain(events.Skip(beforeDefeat).OfType<CardsMoved>(), move =>
            move.Cards.Any(landing => landing.Card == villain.ObjectId));
        Assert.DoesNotContain(events.Skip(beforeDefeat).OfType<FieldSet>(), field =>
            field.Card == identity.ObjectId && field.Verb == "Ready");
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static void PutNemesis(World world, int player, string zone)
    {
        var card = Assert.Single(world.Seats[player].NemesisSet, candidate => Cards.IsNemesisMinion(candidate.FaceId));
        Area? destination = zone switch
        {
            "deck" => world.AreaOf(DeckType.EncounterDeck),
            "discard" => world.AreaOf(DeckType.EncounterDiscardPile),
            "play" => world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)),
            _ => null,
        };
        if (destination is not null) World.MoveToTop(card, destination);
    }

    private static void End(World world, int round, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), round, 4));
        Sequence.Finish(world, Cards, world.Abilities, events);
    }

    internal static World Board(bool expert, bool revealChoice = false, bool captain = false)
    {
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints(expert, captain), [captain ? "Captain America" : "Spider-Man", "Black Widow"], 42, expert: expert);
        var rows = AbilityRows(expert, revealChoice);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        return world;
    }

    private static List<CardBlueprint> Blueprints(bool expert, bool captain = false)
    {
        var blueprints = new List<CardBlueprint>
        {
            new(captain ? "03001b,03001a" : "01001b,01001a", SetupSlot.Identity, 0), new("08001b,08001a", SetupSlot.Identity, 1),
            new(captain ? "03028" : "01167", SetupSlot.Nemesis, 0), new("08026", SetupSlot.Nemesis, 1),
            new("11007a,11007b", SetupSlot.MainScheme, -1),
            new(expert ? "11034" : "11001", SetupSlot.Villain, -1),
            new("11008a,11008b", SetupSlot.SetAside, -1),
            new("11013a,11013b", SetupSlot.SetAside, -1),
            new(expert ? "11039" : "11006", SetupSlot.SetAside, -1),
        };
        for (int i = 0; i < 4; i++)
        {
            blueprints.Add(new($"{11009 + i}a,{11009 + i}b", SetupSlot.SetAside, -1));
            blueprints.Add(new((expert ? 11035 + i : 11002 + i).ToString(System.Globalization.CultureInfo.InvariantCulture), SetupSlot.SetAside, -1));
        }
        for (int i = 0; i < 12; i++) blueprints.Add(new("01101", SetupSlot.Encounter, -1));
        return blueprints;
    }

    internal static List<object> AbilityRows(bool expert, bool revealChoice, bool completion = false)
    {
        var rows = new List<object>
        {
            Row(expert ? "11034" : "11001", "WhenDefeated", "captureKangStageOneDefeat"),
            Row("11008a", "WhenRevealed", "splitKangStageTwo"),
            new { card = "11008b", abilities = Array.Empty<object>() },
            Row("11013a", "WhenRevealed", "revealKangStageThree"),
            Row("11013b", "WhenRevealed", "putKangNemesesIntoPlay"),
            RevealObserver("01167"), RevealObserver("08026"),
        };
        for (int i = 0; i < 4; i++)
        {
            rows.Add(Row((expert ? 11035 + i : 11002 + i).ToString(System.Globalization.CultureInfo.InvariantCulture), "WhenDefeated", "captureKangStageTwoDefeat"));
            rows.Add(Row($"{11009 + i}a", "WhenRevealed", "createKangGameArea"));
            rows.Add(completion ? new { card = $"{11009 + i}b", abilities = new[] { new {
                trigger = new { @event = "WhenMainSchemeCompleted", timing = "ForcedResponse", subject = "this" },
                effect = new { captureKangLocalCompletion = "this" } } } }
                : (object)new { card = $"{11009 + i}b", abilities = Array.Empty<object>() });
        }
        // III's attack interrupt remains outside this nonpublic overlay.
        if (!revealChoice) rows.Add(Row(expert ? "11039" : "11006", "WhenDefeated", "playersWin"));
        if (revealChoice) rows.Add(new { card = "11006", abilities = new[] { new {
            trigger = new { @event = "WhenCardRevealed", timing = "WhenRevealed", subject = "this" },
            effect = new { choose = new { options = new object[] {
                new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } },
                new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 2 } } } } } } } });
        return rows;
    }

    // Synthetic reveal observers distinguish put-into-play from a false reveal.
    private static object RevealObserver(string card) => new { card, abilities = new[] { new {
        trigger = new { @event = "WhenCardRevealed", timing = "WhenRevealed", subject = "this" },
        effect = new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } } } } };

    private static object Row(string card, string timing, string effect) => new { card, abilities = new[] { new {
        trigger = new { @event = timing == "WhenDefeated" ? "WhenCardDefeated" : "WhenCardRevealed", timing, subject = "this" },
        effect = new Dictionary<string, string> { [effect] = effect == "playersWin" ? "game" : "this" } } } };
}
