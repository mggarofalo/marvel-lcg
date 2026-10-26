using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual-row boundaries on nonpublic multiplayer boards.</summary>
public sealed class DoctorStrangeFinalSupportReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-deck.4")]
    [Rule("rr:ownership-and-control.7.2")]
    public void CleaEmptyOwnerDeckDistinguishesShuffleFromDeclinedDefeat(bool accept)
    {
        // "The deck does not reset until there is at least one card in the player's
        // discard pile"; a departing card uses "its owner's equivalent out-of-play area".
        var world = ThorActualFixture.Board("06001a", 3);
        foreach (var card in world.Seats[2].Deck.Cards.ToArray())
            World.MoveToTop(card, world.Seats[2].Hand);
        var source = world.CreateCard("09013",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 2));
        int[] encounter = [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];
        int[] controller = [.. world.Seats[1].Deck.Cards.Select(card => card.ObjectId)];
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, world.Facts, new(source, source, 2, "Clea", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(Assert.Single(prompt.Affordances).Id) : Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.Same(source, Assert.Single(world.Seats[2].Deck.Cards));
        Assert.Equal(controller, world.Seats[1].Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(accept ? encounter : encounter[..^1],
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId));
        Assert.Equal(accept ? 0 : 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(2)).Cards.Count);
        Assert.Equal(accept ? 0 : 1, events.OfType<CardsMoved>().Count(move => move.Verb == "Reset"));
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:uses-x-type")]
    [Rule("rr:and.1")]
    public void LastMedicalUseCompletesBothIndependentEffectsInEitherOrder(bool statusFirst)
    {
        // Uses: "When the last all-purpose counter is removed ... discard that card."
        // And: "Resolve as much of each effect as possible."
        var (world, runner, source) = NightNurseStatusHostTests.Board(counters: 1, controller: 1);
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(2);
        var selected = Statuses.Give(world, hero, Statuses.Tough);
        var retained = Statuses.Give(world, hero, Statuses.Confused);
        var action = Assert.Single(runner.Actions(world, 1), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(0), source.Area.PlayArea);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(hero.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances).Id, statusFirst ? [1, 0] : [0, 1]), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(statusFirst ? 2 : 1, hero.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(selected.ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.RemovedArea, selected.Area.Type);
        Assert.Equal(DeckType.StatusArea, retained.Area.Type);
        Assert.Equal(0, source.Tokens["c_medical"]);
        Assert.Contains(events.OfType<FieldSet>(), change => change.Card == source.ObjectId && change.Verb == "Exhaust");
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.Agenda.IsBusy);
    }
}
