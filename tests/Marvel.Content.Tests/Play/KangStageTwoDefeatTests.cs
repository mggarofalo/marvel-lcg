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

// Nonpublic actual-row overlay: complete Kang admission and central reunion remain closed.
public sealed class KangStageTwoDefeatTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11002", "11009b", 0, PhaseTransitionKind.EndPlayerPhase)]
    [InlineData("11003", "11010b", 1, PhaseTransitionKind.EndVillainPhase)]
    [InlineData("11004", "11011b", 0, PhaseTransitionKind.EndVillainPhase)]
    [InlineData("11005", "11012b", 1, PhaseTransitionKind.EndPlayerPhase)]
    [InlineData("11035", "11009b", 1, PhaseTransitionKind.EndPlayerPhase)]
    [InlineData("11036", "11010b", 0, PhaseTransitionKind.EndVillainPhase)]
    [InlineData("11037", "11011b", 1, PhaseTransitionKind.EndVillainPhase)]
    [InlineData("11038", "11012b", 0, PhaseTransitionKind.EndPlayerPhase)]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void PrintedDefeatRemovesItsSchemeThenWaitsForActualPhaseEnd(
        string face, string schemeFace, int first, PhaseTransitionKind boundary)
    {
        // "A defeated card leaves play after its When Defeated ability is resolved."
        // 11002-11005/11035-11038: remove the named scheme; at phase end join another area.
        var (world, kang, scheme) = Board(face, schemeFace, first);
        var source = world.GameAreaOf(PlayArea.Of(0));
        var destination = world.GameAreaOf(PlayArea.Of(1));
        var attachment = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), kang.ObjectId));
        Statuses.Give(world, kang, Statuses.Tough);
        var left = world.CreateCard("01011", world.Seats[0].Hero);
        var right = world.CreateCard("01011", world.Seats[1].Hero);
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var capture = Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(kang.ObjectId, capture.Defeated.Card);
        Assert.True(capture.NextPhase);
        Assert.Equal(new[] { PlayArea.Of(0) }, capture.Group);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.False(Statuses.Has(world, kang, Statuses.Tough));
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Equal(Outcome.Unfinished, world.Result);
        int schemeRemoved = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(c => c.Card == scheme.ObjectId));
        int kangRemoved = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(c => c.Card == kang.ObjectId));
        Assert.True(schemeRemoved >= 0 && kangRemoved > schemeRemoved);
        // A non-ending transition cannot consume the capture.
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 7, 3));
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Single(world.Agenda.KangTransitions.Pending);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(boundary), 7, 6));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(first, prompt.Player);
        Assert.Equal(destination!.Id, Assert.Single(prompt.Affordances).Id);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(destination.Id), events);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(new[] { left.ObjectId, right.ObjectId }, prompt.Affordances.Select(item => item.Id));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(left.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Equal(DeckType.DiscardPile, left.Area.Type);
        Assert.Same(world.Seats[1].Hero, right.Area);
        Assert.Single(events.OfType<PlayAreaJoined>());
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Empty(world.Agenda.Outstanding);
        int count = events.Count;
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(count, events.Count);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:forced.5")]
    public void OwnDefeatSharesNormalForcedOrderingBeforeVillainDeparture(bool observerFirst)
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order."
        var (world, kang, scheme) = Board("11004", "11011b", 1, observer: true);
        var attached = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), kang.ObjectId));
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(Question.Order, prompt.Asking);
        Assert.Equal(1, prompt.Player);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(DeckType.VillainArea, kang.Area.Type);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Sequence.Answer(world, Cards, world.Abilities, prompt,
            Decision.Take(observerFirst ? attached.ObjectId : kang.ObjectId), []);
        Sequence.Finish(world, Cards, world.Abilities, []);
        Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void MissingMatchingLocalSchemeFailsWithoutCaptureRemovalOrVictory()
    {
        var (world, kang, scheme) = Board("11004", "11010b", 0);
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => {
            Defeat.Character(world, Cards, kang, "prepared defeat", []);
            Sequence.Finish(world, Cards, world.Abilities, []);
        });
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(DeckType.VillainArea, kang.Area.Type);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void ActualFirstSplitAndSecondStageDefeatReconstructEveryDecisionBoundary()
    {
        var (first, firstKang, _, _) = KangReunionFixture.Board(1, false);
        var (second, secondKang, _, _) = KangReunionFixture.Board(1, false);
        List<string> Run(World world, Card kang)
        {
            var snapshots = new List<string>();
            var events = new List<GameEvent>();
            void Observe()
            {
                snapshots.Add(world.Digest().Canonical());
                snapshots.Add(string.Join(";", world.GameAreas.Select(area => $"{area.Id}:"
                    + string.Join(",", area.PlayAreas.OrderBy(play => play.Player)))));
                snapshots.Add(JsonSerializer.Serialize(world.Agenda.KangTransitions.Pending.Select(item => new {
                    item.Defeated.Card, item.Defeated.Incarnation, Source = item.Source.Id,
                    Group = item.Group.Select(play => play.Player), item.Round, item.Boundary, item.NextPhase })));
                snapshots.Add(JsonSerializer.Serialize(world.Agenda.Outstanding.Select(step => new {
                    step.What, step.Round, step.Number, step.Subject, step.Seat })));
                snapshots.Add(JsonSerializer.Serialize(world.Random.Generator.GetState()));
                snapshots.Add(world.Random.Generator.WordsConsumed.ToString(System.Globalization.CultureInfo.InvariantCulture));
                snapshots.Add(string.Join(";", events.Select(item => JsonSerializer.Serialize(item, item.GetType()))));
            }
            Agendas.Happening(world);
            Defeat.Character(world, Cards, kang, "prepared stage-I defeat", events);
            Sequence.Finish(world, Cards, world.Abilities, events);
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));
            Sequence.Finish(world, Cards, world.Abilities, events);
            var local = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)).Cards);
            Assert.Equal("11004", local.FaceId);
            var left = world.CreateCard("01011", world.Seats[1].Hero);
            world.CreateCard("01011", world.Seats[0].Hero);
            Agendas.Happening(world);
            Defeat.Character(world, Cards, local, "prepared stage-II defeat", events);
            Sequence.Finish(world, Cards, world.Abilities, events);
            Observe();
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
            Observe();
            Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), events);
            prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
            Observe();
            Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(left.ObjectId), events);
            Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
            Observe();
            Assert.Equal(3, world.Random.Generator.WordsConsumed);
            Assert.Same(world.GameAreaOf(PlayArea.Of(0)), world.GameAreaOf(PlayArea.Of(1)));
            Assert.Empty(world.Agenda.Outstanding);
            Assert.Empty(world.Agenda.KangTransitions.Pending);
            Assert.Equal(Outcome.Unfinished, world.Result);
            return snapshots;
        }
        Assert.Equal(Run(first, firstKang), Run(second, secondKang));
    }

    private static (World World, Card Kang, Card Scheme) Board(string face, string schemeFace, int first, bool observer = false)
    {
        var world = new World(Cards, 2, 42) { FirstPlayer = first };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a,01001b" : "01019a,01019b", seat.Hero);
            world.CreateCard("01089", seat.Deck);
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        world.Detach(PlayArea.Villains, "prepared split", []);
        var kang = world.CreateCard(face, world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var scheme = world.CreateCard(schemeFace, world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        world.CreateCard("11010b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)));
        var rows = new List<object> { new { card = face, abilities = new[] { new {
            trigger = new { @event = "WhenCardDefeated", timing = "WhenDefeated", subject = "this" },
            effect = new { captureKangStageTwoDefeat = "this" } } } } };
        if (observer) rows.Add(new { card = "01099", abilities = new[] { new {
            trigger = new { @event = "WhenCardDefeated", timing = "ForcedInterrupt", subject = "attachedTo" },
            effect = new { dealDamage = new { cards = new { titled = "Spider-Man" }, amount = 1 } } } } });
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        return (world, kang, scheme);
    }
}
