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

// Prepared nonpublic actual-card slice. Only the reached printed abilities are
// overlaid; no complete Kang admission, stage-3 completion or public setup claim.
public sealed class KangFirstSplitTests : CoreCardTestBase
{
    private static readonly string[] ExpectedStages = ["11011b", "11009b", "11010b", "11012b"];

    [Theory]
    [InlineData(0, false, 2)]
    [InlineData(0, false, 1)]
    [InlineData(2, false, 3)]
    [InlineData(3, false, 4)]
    [InlineData(1, false, 2)]
    [InlineData(0, true, 2)]
    [InlineData(0, true, 1)]
    [InlineData(2, true, 3)]
    [InlineData(3, true, 4)]
    [InlineData(1, true, 2)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:toughness.1")]
    public void DefeatWaitsForThePhaseThenRevealsDistinctLocalStagesInTurnOrder(int firstPlayer, bool expert, int players)
    {
        // 11001/11034: "Advance the main scheme to stage 2 at the end of the phase."
        // 11008a: "Each player reveals a random stage 3A in turn order."
        var (world, kang, side, minions) = Board(firstPlayer, expert, players: players);
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        DamagePlacement.Resolve(world, Cards, new DamageRequest(world.Seats[0].IdentityCard, kang,
            DamagePlacement.Health(world, Cards, kang), "prepared lethal damage", "Damage", 0), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.NotNull(world.Agenda.KangSplit.Pending);
        Assert.Equal("11007b", world.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
        Assert.Equal(DeckType.SideSchemesArea, side.Area.Type);
        Assert.Single(world.GameAreas);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.Equal(Outcome.Unfinished, world.Result);

        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));
        Sequence.Finish(world, Cards, world.Abilities, events);

        var central = world.Cards.Single(card => card.FaceId == "11008b");
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        Assert.Equal(1, central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(DeckType.EncounterDiscardPile, side.Area.Type);
        Assert.Null(world.Agenda.KangSplit.Pending);
        var localSchemes = world.PlayerOrder.Select(player =>
            Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)).Cards)).ToArray();
        // MT19937 seed 42: Choice([9,10,11,12])=11; Choice([9,10,12])=9,
        // including masked rejection for the second draw. This pins consumption,
        // distinct allocation and first-player order independently of the controller.
        Assert.Equal(ExpectedStages.Take(players), localSchemes.Select(card => card.FaceId));
        Assert.Equal(new long[] { 1, 3, 4, 5 }[players - 1], world.Random.Generator.WordsConsumed);
        foreach (int player in world.PlayerOrder)
        {
            var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)).Cards);
            var villain = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)).Cards);
            Assert.Equal(expert ? 22 : 18, DamagePlacement.Health(world, Cards, villain));
            Assert.True(Statuses.Has(world, villain, Statuses.Tough));
            Assert.Equal(Cards.PrintedValue(scheme.FaceId, "StartingThreat", players), scheme.Tokens.GetValueOrDefault("k_threat"));
            Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards);
            Assert.Same(world.GameAreaOf(PlayArea.Of(player)), Places.GameAreaOf(world, minions[player]));
            foreach (int other in world.PlayerOrder.Where(other => other != player))
                Assert.False(Places.CanAffect(world, villain, minions[other]));
        }
        Assert.Equal(4 - players, world.Cards.Count(card => card.Area.Type == DeckType.RemovedArea
            && card.Faces.Any(face => face is "11009a" or "11010a" or "11011a" or "11012a")));
        int finalFlip = events.FindLastIndex(item => item is CardsFlipped flipped && flipped.Cards.Contains(central.ObjectId));
        int lastDeal = events.FindLastIndex(item => item.Verb == "Deal");
        Assert.True(finalFlip > lastDeal);
        int firstUnusedRemoval = events.FindIndex(item => item is CardsMoved moved
            && moved.Cards.Any(landing => world.Cards[landing.Card].Area.Type == DeckType.RemovedArea
                && world.Cards[landing.Card].Faces.Any(face => face is "11009a" or "11010a" or "11011a" or "11012a")));
        if (players < 4)
        {
            Assert.True(firstUnusedRemoval > lastDeal);
            Assert.True(firstUnusedRemoval < finalFlip);
        }
        else Assert.Equal(-1, firstUnusedRemoval);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:forced.5")]
    public void OwnWhenDefeatedSharesOrderingWithAnAttachedForcedInterrupt(bool attachmentFirst)
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order."
        var (world, kang, _, _) = Board(1, false, observer: true);
        var attachment = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, kang.ObjectId));
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared lethal damage", events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(1, prompt.Player);
        Assert.Equal(Question.Order, prompt.Asking);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.Null(world.Agenda.KangSplit.Pending);
        Assert.Equal(DeckType.VillainArea, kang.Area.Type);
        Sequence.Answer(world, Cards, world.Abilities, prompt,
            Decision.Take(attachmentFirst ? attachment.ObjectId : kang.ObjectId), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.NotNull(world.Agenda.KangSplit.Pending);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void ReconstructedDecisionsRetainTopologyEventsRngAndExactOnceBoundary()
    {
        var (first, firstKang, _, _) = Board(1, false);
        var (second, secondKang, _, _) = Board(1, false);
        List<GameEvent> Run(World world, Card kang)
        {
            var events = new List<GameEvent>();
            Agendas.Happening(world);
            DamagePlacement.Resolve(world, Cards, new DamageRequest(world.Seats[0].IdentityCard, kang,
                DamagePlacement.Health(world, Cards, kang), "prepared lethal damage", "Damage", 0), events);
            Sequence.Finish(world, Cards, world.Abilities, events);
            Assert.NotNull(world.Agenda.KangSplit.Pending);
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
            Sequence.Finish(world, Cards, world.Abilities, events);
            return events;
        }
        var expected = Run(first, firstKang);
        var actual = Run(second, secondKang);
        Assert.Equal(first.Digest().Canonical(), second.Digest().Canonical());
        Assert.Equal(first.GameAreas.Select(area => string.Join(",", area.PlayAreas.OrderBy(play => play.Player))),
            second.GameAreas.Select(area => string.Join(",", area.PlayAreas.OrderBy(play => play.Player))));
        Assert.Equal(expected.Select(item => JsonSerializer.Serialize(item, item.GetType())),
            actual.Select(item => JsonSerializer.Serialize(item, item.GetType())));
        Assert.Equal(first.Random.Generator.GetState().Index, second.Random.Generator.GetState().Index);
        Assert.Equal(first.Random.Generator.GetState().Words, second.Random.Generator.GetState().Words);
        Assert.Equal(first.Random.Generator.WordsConsumed, second.Random.Generator.WordsConsumed);
        int count = actual.Count;
        Sequence.Finish(second, Cards, second.Abilities, actual);
        Assert.Equal(count, actual.Count);
        Assert.Empty(second.Agenda.Outstanding);
        Assert.Null(second.Agenda.KangSplit.Pending);
    }

    [Fact]
    public void SeededAssignmentsReachEveryPrintedStartingThreatAndMatchingKang()
    {
        var reached = new HashSet<string>(StringComparer.Ordinal);
        for (uint seed = 0; seed < 6; seed++)
        {
            var (world, kang, _, _) = Board(0, false, seed: seed);
            Agendas.Happening(world);
            Defeat.Character(world, Cards, kang, "prepared defeat", []);
            Sequence.Finish(world, Cards, world.Abilities, []);
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));
            Sequence.Finish(world, Cards, world.Abilities, []);
            foreach (int player in world.PlayerOrder)
            {
                var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)).Cards);
                var villain = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)).Cards);
                reached.Add(scheme.FaceId);
                var (expectedVillain, expectedThreat) = scheme.FaceId switch
                {
                    "11009b" => ("11002", 0), "11010b" => ("11003", 1),
                    "11011b" => ("11004", 1), "11012b" => ("11005", 2),
                    _ => throw new InvalidOperationException("unexpected stage 3"),
                };
                Assert.Equal(expectedVillain, villain.FaceId);
                Assert.Equal(expectedThreat, scheme.Tokens.GetValueOrDefault("k_threat"));
            }
        }
        Assert.Equal(4, reached.Count);
    }

    private static (World World, Card Kang, Card Side, Card[] Minions) Board(int first, bool expert, bool observer = false, uint seed = 42, int players = 2)
    {
        var world = new World(Cards, players, seed) { FirstPlayer = first };
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            string[] identities = ["01001a,01001b", "01019a,01019b", "01029a,01029b", "01040a,01040b"];
            seat.IdentityCard = world.CreateCard(identities[player], seat.Hero);
        }
        var scheme = world.CreateCard("11007a,11007b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.TurnTo("11007b");
        var kang = world.CreateCard(expert ? "11034" : "11001", world.AreaOf(DeckType.VillainArea));
        var aside = world.AreaOf(DeckType.AsideDeck);
        world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesDeck));
        for (int index = 0; index < 4; index++)
        {
            world.CreateCard($"{11009 + index}a,{11009 + index}b", aside);
            world.CreateCard((expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), aside);
        }
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea));
        Card[] minions = Enumerable.Range(0, players).Select(player => world.CreateCard("01101",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)))).ToArray();
        for (int count = 0; count < 6; count++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var rows = new List<object>();
        object Row(string card, string timing, string effect) => new { card, abilities = new[] { new {
            trigger = new { @event = timing == "WhenDefeated" ? "WhenCardDefeated" : "WhenCardRevealed", timing, subject = "this" },
            effect = new Dictionary<string, string> { [effect] = "this" } } } };
        rows.Add(Row(kang.FaceId, "WhenDefeated", "captureKangStageOneDefeat"));
        rows.Add(Row("11008a", "WhenRevealed", "splitKangStageTwo"));
        rows.Add(new { card = "11008b", abilities = Array.Empty<object>() });
        for (int index = 0; index < 4; index++)
        {
            rows.Add(Row($"{11009 + index}a", "WhenRevealed", "createKangGameArea"));
            rows.Add(new { card = $"{11009 + index}b", abilities = Array.Empty<object>() });
        }
        if (observer) rows.Add(new { card = "01099", abilities = new[] { new {
            trigger = new { @event = "WhenCardDefeated", timing = "ForcedInterrupt", subject = "attachedTo" },
            effect = new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } } } } });
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        return (world, kang, side, minions);
    }
}
