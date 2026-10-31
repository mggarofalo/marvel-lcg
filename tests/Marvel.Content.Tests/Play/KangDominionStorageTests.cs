using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic actual 3B completion to 4A storage/reveal; no production admission.
public sealed class KangDominionStorageTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, true)]
    [Rule("rr:reveal.step.1")]
    [Rule("rr:reveal.step.2")]
    public void ActualLocalCompletionStoresOnePhysicalDominionUntilOrderedCentralReveal(bool expert, bool multiple, bool stale, bool villainPhase)
    {
        // 11009b-11012b: "place 1 set-aside Kang's Dominion facedown under stage 4A."
        // 11013a: "Reveal Kang (III) ... Reveal each face down Kang's Dominion under this stage."
        var world = KangCentralTransitionTests.Board(expert);
        var rows = KangCentralTransitionTests.AbilityRows(expert, false, completion: true);
        rows.Add(multiple ? new { card = "11023", abilities = new[] { new {
            trigger = new { @event = "WhenCardRevealed", timing = "WhenRevealed", subject = "this" },
            effect = new { choose = new { options = new object[] {
                new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } },
                new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 2 } } } } } } } }
            : (object)new { card = "11023", abilities = Array.Empty<object>() });
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        var dominions = Enumerable.Range(0, 4).Select(_ => world.CreateCard("11023", world.AreaOf(DeckType.AsideDeck))).ToArray();
        var events = new List<GameEvent>();
        var initial = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, initial, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        End(world, 1, events);
        var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)).Cards);
        var local = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)).Cards);
        var host = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        int oldIncarnation = host.Incarnation;
        Threat.Schedule(world, new ThreatBatch([scheme], null, 9, ThreatCause.CardAbility, "test", 0));
        Sequence.Finish(world, Cards, world.Abilities, events);
        var stored = Assert.Single(BeneathCards.Of(world, host));
        Assert.Same(dominions[0], stored);
        Assert.False(stored.FaceUp);
        Assert.Equal(DeckType.BeneathCardsPile, stored.Area.Type);
        Assert.Equal(host.ObjectId, stored.Area.Host);
        Assert.Equal(-1, stored.Owner);
        Assert.Equal(0, stored.Incarnation);
        Assert.Equal(DeckType.MainSchemesDeck, host.Area.Type);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Equal(DeckType.VillainArea, local.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("is_completed"));
        int beforeDuplicate = events.Count;
        Assert.Throws<RulesNotImplementedException>(() => KangLocalCompletion.Capture(world, scheme,
            new Occurrence(999, [MainSchemeCompletionOperation.Completed], scheme.ObjectId), events));
        Assert.Single(BeneathCards.Of(world, host));
        Assert.Equal(beforeDuplicate, events.Count);
        Assert.Equal(Outcome.Unfinished, world.Result);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(villainPhase ? PhaseTransitionKind.EndVillainPhase : PhaseTransitionKind.EndPlayerPhase), 2, 4));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(DeckType.RemovedArea, local.Area.Type);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var remaining = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)).Cards);
        Agendas.Happening(world);
        if (multiple)
        {
            var lastScheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)).Cards);
            Threat.Schedule(world, new ThreatBatch([lastScheme], null, 9, ThreatCause.CardAbility, "test", 1));
        }
        else Defeat.Character(world, Cards, remaining, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        if (multiple)
        {
            if (!FinishStoredReveals(world, host, dominions, stale, events)) return;
        }
        else End(world, 3, events);
        Assert.Equal("11013b", host.FaceId);
        Assert.Equal(oldIncarnation + 1, host.Incarnation);
        Assert.Empty(BeneathCards.Of(world, host));
        Assert.Equal(DeckType.SideSchemesArea, stored.Area.Type);
        Assert.True(stored.FaceUp);
        Assert.Equal(1, stored.Incarnation);
        Assert.Equal(6, stored.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(dominions.Skip(multiple ? 2 : 1), card => Assert.Equal(DeckType.AsideDeck, card.Area.Type));
        if (multiple)
        {
            Assert.Equal(DeckType.SideSchemesArea, dominions[1].Area.Type);
            Assert.Equal(dominions.Take(2).Select(card => card.ObjectId), events.OfType<CardsFlipped>()
                .SelectMany(flip => flip.Cards).Where(id => dominions.Take(2).Any(card => card.ObjectId == id)));
        }
        AssertCentralRevealOrder(world, expert, host, stored, events);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData("11013a", "01101", DeckType.BeneathCardsPile)]
    [InlineData("11013b", "11023", DeckType.BeneathCardsPile)]
    [InlineData("11013a", "11023", DeckType.UpgradesArea)]
    [InlineData("01097a", "11023", DeckType.BeneathCardsPile)]
    public void HostExceptionRejectsOtherPhysicalPairsWithoutMovingCards(string hostFace, string face, DeckType type)
    {
        var world = Hero("01001a");
        var host = world.CreateCard(hostFace, world.AreaOf(DeckType.MainSchemesDeck));
        var card = world.CreateCard(face, world.AreaOf(DeckType.AsideDeck));
        var destination = world.AreaOf(type, PlayArea.Villains, host.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Throws<InvalidOperationException>(() => World.MoveToTop(card, destination));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(destination.Cards);
    }

    [Fact]
    public void UnrevealedHostThatAlreadyEnteredPlayCannotReviveItsOldStorage()
    {
        var world = Hero("01001a");
        var aside = world.AreaOf(DeckType.AsideDeck);
        var deck = world.AreaOf(DeckType.MainSchemesDeck);
        var host = world.CreateCard("11013a,11013b", deck);
        var card = world.CreateCard("11023", aside);
        var storage = world.AreaOf(DeckType.BeneathCardsPile, PlayArea.Villains, host.ObjectId);
        World.MoveToTop(host, world.AreaOf(DeckType.MainSchemesArea));
        World.MoveToTop(host, deck);
        string before = world.Digest().Canonical();
        Assert.Throws<InvalidOperationException>(() => World.MoveToTop(card, storage));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(1, host.Incarnation);
        Assert.Empty(storage.Cards);
    }

    private static void AssertCentralRevealOrder(World world, bool expert, Card host, Card stored,
        List<GameEvent> events)
    {
        int villain = events.FindLastIndex(item => item is CardsMoved move && move.Cards.Any(landing =>
            landing.Card == world.Cards.Single(card => card.FaceId == (expert ? "11039" : "11006")).ObjectId));
        int reveal = events.FindIndex(item => item is CardsFlipped flip && flip.Cards.Contains(stored.ObjectId));
        int fourB = events.FindIndex(item => item is CardsFlipped flip && flip.Cards.Contains(host.ObjectId));
        Assert.True(villain >= 0 && reveal > villain && fourB > reveal);
        Assert.Single(events.OfType<CardDetached>(), item => item.Card == stored.ObjectId && item.Host == host.ObjectId);
    }

    private static bool FinishStoredReveals(World world, Card host, Card[] dominions, bool stale,
        List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 3, 4));
        for (int index = 0; index < 2; index++)
        {
            var revealChoice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
            Assert.Equal("11013a", host.FaceId);
            Assert.Equal(index + 1, dominions.Count(card => card.Area.Type == DeckType.SideSchemesArea));
            Assert.Equal(1 - index, BeneathCards.Of(world, host).Count);
            if (index == 0) Assert.False(dominions[1].FaceUp);
            Assert.DoesNotContain(events.OfType<CardsFlipped>(), flip => flip.Cards.Contains(host.ObjectId));
            Sequence.Answer(world, Cards, world.Abilities, revealChoice, Decision.Take(revealChoice.Affordances[0].Id), events);
            if (stale && index == 0)
            {
                // Synthetic departure/reentry isolates a stale physical copy
                // while the first real Dominion reveal is suspended.
                var storedArea = dominions[1].Area;
                World.MoveToTop(dominions[1], world.AreaOf(DeckType.SideSchemesArea));
                World.MoveToTop(dominions[1], storedArea);
                var error = Assert.Throws<RulesNotImplementedException>(() =>
                    Sequence.Work(world, Cards, world.Abilities, events));
                Assert.Contains("original concealed physical storage", error.Message);
                Assert.False(dominions[1].FaceUp);
                Assert.Equal("11013a", host.FaceId);
                return false;
            }
        }
        Sequence.Finish(world, Cards, world.Abilities, events);
        return true;
    }

    private static void End(World world, int round, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), round, 4));
        Sequence.Finish(world, world.Facts, world.Abilities, events);
    }
}
