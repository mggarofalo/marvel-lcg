using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousSchemeDefeatTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:simultaneous-resolution")]
    public void InconspicuousRemovesAllSharesOnceAndFirstPlayerOrdersDefeatAbilities(bool patrolFirst)
    {
        // "The first player determines the order in which the effects resolve."
        // ruling:f216f6359fb0bf96 and ruling:666430d10d44fb0b establish simultaneous split threat removal.
        var world = Hero("01001a", 2);
        world.FirstPlayer = 1;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var patrol = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        patrol.PlaceTokens("k_threat", 1);
        var subjects = world.CreateCard("04123", world.AreaOf(DeckType.SideSchemesArea));
        subjects.PlaceTokens("k_threat", 2);
        for (int copy = 0; copy < 4; copy++) world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("04038", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var allocation = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, allocation,
            Decision.Take(Assert.Single(allocation.Affordances).Id, [patrol.ObjectId, subjects.ObjectId, subjects.ObjectId], []), events);
        var ordering = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(0, patrol.Tokens["k_threat"]);
        Assert.Equal(0, subjects.Tokens["k_threat"]);
        Assert.Equal(1, ordering.Player);
        Assert.Equal(2, ordering.Affordances.Count);
        var selected = Assert.Single(ordering.Affordances,
            offer => offer.AnchorId == (patrolFirst ? patrol.ObjectId : subjects.ObjectId));
        Sequence.Answer(world, Cards, runner, ordering, Decision.Take(selected.Id), events);
        Finish(world, runner, patrolFirst, events);
        Assert.Equal(DeckType.EncounterDiscardPile, patrol.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, subjects.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(2, events.OfType<FieldSet>().Count(change => change.Field == "k_threat"));
        Assert.Single(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(2, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:simultaneous-resolution")]
    public void SyntheticGroupedRemovalRetainsBothClearReceiptsAcrossBothDefeatAbilities()
    {
        // Synthetic composition tests the receipt contract; actual encounter abilities remain authored.
        // "The first player determines the order in which the effects resolve."
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"thwart":{"target":{"titled":"Hydra Patrol"},"effect":{"seq":[
              {"removeThreat":{"scheme":{"query":"schemes"},"amount":2}},
              {"if":{"test":{"atLeast":{"value":{"result":"schemesCleared"},"count":2}},
                "then":{"draw":{"player":"you","count":1}}}}
            ]}}}
            """, includeAuthored: true);
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var source = InPlay(world, AuthoredCards.AuntMay);
        var patrol = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        patrol.PlaceTokens("k_threat", 1);
        var subjects = world.CreateCard("04123", world.AreaOf(DeckType.SideSchemesArea));
        subjects.PlaceTokens("k_threat", 2);
        for (int copy = 0; copy < 4; copy++) world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01089", world.Seats[0].Deck);
        var top = world.CreateCard("01090", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var ordering = Sequence.Work(world, Cards, runner, events)!;
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(2, ordering.Affordances.Count);
        var selected = Assert.Single(ordering.Affordances, offer => offer.AnchorId == subjects.ObjectId);
        Sequence.Answer(world, Cards, runner, ordering, Decision.Take(selected.Id), events);
        Finish(world, runner, patrolFirst: false, events);
        Assert.Same(top, Assert.Single(world.Seats[0].Hand.Cards));
        Assert.Equal(2, events.OfType<FieldSet>().Count(change => change.Field == "k_threat"));
        Assert.Equal(DeckType.EncounterDiscardPile, patrol.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, subjects.Area.Type);
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void SyntheticExternalInterruptTriggersOnceForEachSimultaneouslyDefeatedScheme()
    {
        // "Each interrupt ability can only be triggered once per occurrence of its triggering condition."
        // Two defeated-card facts share one window and must not re-trigger on the enclosing thwart.
        var runner = Runner(AuthoredCards.AuntMay, "ForcedInterrupt", """
            {"if":{"test":{"isKind":{"card":"defeated","kind":"sideScheme"}},
              "then":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}}
            """, eventName: "WhenCardDefeated", includeAuthored: true);
        var world = Hero("01001a");
        world.Abilities = runner;
        InPlay(world, AuthoredCards.AuntMay);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var first = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        first.PlaceTokens("k_threat", 1);
        second.PlaceTokens("k_threat", 1);
        Agendas.Happening(world);
        var events = new List<GameEvent>();
        var result = ThreatBatchRemoval.Remove(world, runner,
            [new(first, 1, "synthetic thwart", "Remove_Threat", 0),
             new(second, 1, "synthetic thwart", "Remove_Threat", 0)], events);
        Assert.All(result, removal => Assert.True(removal.Suspended));
        var order = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, order.Affordances.Count);
        Sequence.Answer(world, Cards, runner, order, Decision.Take(order.Affordances[0].Id), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, events.OfType<FieldSet>().Count(change => change.Card == main.ObjectId));
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Finish(World world, Marvel.Cards.Run.AbilityRunner runner, bool patrolFirst, List<GameEvent> events)
    {
        int searches = 0;
        for (int step = 0; step < 12; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) { Assert.Equal(2, searches); return; }
            Assert.Empty(world.Seats[0].Hand.Cards);
            var first = prompt.Affordances[0];
            if (prompt.Asking == Question.Order && first.Targets is { } targets)
            {
                Sequence.Answer(world, Cards, runner, prompt, Decision.Take(first.Id, targets.Legal, []), events);
                continue;
            }
            Assert.All(prompt.Affordances, offer => Assert.Equal("01182", world.Cards[offer.AnchorId].FaceId));
            if (searches == 0)
                Assert.Equal(patrolFirst ? 0 : 1, world.Areas.Where(area => area.Type == DeckType.EngagedEnemiesArea).Sum(area => area.Cards.Count));
            searches++;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(first.Id), events);
        }
        Assert.Fail("Defeat effects did not finish within their bounded decisions.");
    }
}
