using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LokiRr18OverkillTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:encounter-deck.1")]
    public void CompletedAttackSpillsOnlyAfterLokiActuallyDeparts(bool heals, bool last)
    {
        // "If this attack defeats a minion, deal the damage beyond its hit points to the villain."
        // Damage step 6: "when [character] would be defeated"; step 8: "Discarding of a defeated character."
        // "If the encounter deck is empty, the encounter discard pile is immediately shuffled."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var discarded = PrepareDiscard(world, heals, last);
        int incarnation = loki.Incarnation;
        world.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, Amount: 1, Affects: hero.ObjectId));
        var events = new List<GameEvent>();
        var result = DamageAttacks.Attack(world, world.Facts, hero, loki, 6, "test", "test", events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.True(result.IsComplete);
        Assert.Equal(6, result.Taken);
        Assert.Equal(heals ? 0 : 2, result.Excess);
        AssertAttackBoard(world, loki, discarded, incarnation, heals, last);
        AssertDiscardBeforeLokiResolution(events, loki, discarded, heals, last);
        if (!heals) AssertDepartureBeforeSpill(events, loki, villain);
    }

    private static void AssertAttackBoard(World world, Card loki, Card discarded, int incarnation, bool heals, bool last)
    {
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(heals ? 0 : 2, villain.Damage);
        Assert.Equal(heals ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(last ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(incarnation, loki.Incarnation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    private static void AssertDiscardBeforeLokiResolution(List<GameEvent> events, Card loki, Card discarded, bool heals, bool last)
    {
        int discard = AssertSingleTopDiscard(events, discarded);
        int reset = events.FindIndex(entry => entry is CardsMoved moved
            && moved.From.Zone == nameof(DeckType.EncounterDiscardPile)
            && moved.To.Zone == nameof(DeckType.EncounterDeck));
        Assert.Equal(last, reset >= 0);
        if (last) Assert.True(reset > discard);
        int resolved = FindLokiResolution(events, loki, heals);
        Assert.True(resolved > discard);
        if (last) Assert.True(resolved > reset);
    }

    private static int AssertSingleTopDiscard(List<GameEvent> events, Card discarded)
    {
        int discard = events.FindIndex(entry => entry is CardsMoved moved
            && moved.From.Zone == nameof(DeckType.EncounterDeck)
            && moved.To.Zone == nameof(DeckType.EncounterDiscardPile)
            && moved.Cards.Any(card => card.Card == discarded.ObjectId));
        Assert.True(discard >= 0);
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.From.Zone == nameof(DeckType.EncounterDeck)
            && moved.To.Zone == nameof(DeckType.EncounterDiscardPile));
        return discard;
    }

    private static int FindLokiResolution(List<GameEvent> events, Card loki, bool heals) =>
        events.FindIndex(entry => heals
            ? entry is FieldSet field && field.Card == loki.ObjectId && field.Field == "health" && field.To == 4
            : entry is CardsMoved moved && moved.Cards.Any(card => card.Card == loki.ObjectId));

    private static void AssertDepartureBeforeSpill(List<GameEvent> events, Card loki, Card villain)
    {
        int departure = events.FindIndex(entry => entry is CardsMoved moved && moved.Cards.Any(card => card.Card == loki.ObjectId));
        int spill = events.FindIndex(entry => entry is FieldSet field && field.Card == villain.ObjectId && field.Field == "health");
        Assert.True(departure >= 0 && spill > departure);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:damage.step.6")]
    public void RelentlessAssaultGivesNoSpillOrGloryWhenLokiHeals(bool heals, bool last)
    {
        // "If this attack defeats a minion" requires defeat after "when [character] would be defeated" interrupts.
        // Printed 06017 requires defeat.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var discarded = PrepareDiscard(world, heals, last);
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var card = world.CreateCard("01053", seat.Hand);
        var payment = world.CreateCard("01090", seat.Hand);
        int[] payments = [payment.ObjectId];
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payments, []));
        int hallResponses = 0;
        for (int step = 0; step < 25; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            var response = prompt.Affordances.SingleOrDefault(option => option.AnchorId == hall.ObjectId);
            if (response is not null) hallResponses++;
            Decision answer = response is not null ? Decision.Take(response.Id)
                : prompt.Affordances.Any(option => option.Id == loki.ObjectId) ? Decision.Take(loki.ObjectId)
                : throw new InvalidOperationException(prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        int defeats = heals ? 0 : 1;
        Assert.Equal(defeats, hallResponses);
        Assert.Equal(defeats, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(defeats, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(heals ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(last ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(0, loki.Damage);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    private static Card PrepareDiscard(World world, bool heals, bool last)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (last)
            foreach (var card in deck.Cards.ToArray())
                World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        // The opposite kind already in discard must not determine Loki's branch after a reset.
        world.CreateCard(heals ? "01101" : "01186", world.AreaOf(DeckType.EncounterDiscardPile));
        return world.CreateCard(heals ? "01186" : "01101", deck);
    }
}
