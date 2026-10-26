using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorLokiCardTests
{
    [Theory]
    [InlineData("01186", false, true)]
    [InlineData("01186", true, true)]
    [InlineData("01101", false, false)]
    [InlineData("01101", true, false)]
    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    public void ActualLokiDiscardsExactlyTopCardAndHealsOnlyForTreachery(string top, bool last, bool heals)
    {
        // "The word 'would' is used to define the triggering condition of some interrupt abilities."
        // FAQ 06028 makes this interrupt mandatory; physical receipt remains valid across reset.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (last) foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        // An older treachery in discard cannot decide the non-treachery branch.
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDiscardPile));
        var discarded = world.CreateCard(top, deck);
        long incarnation = loki.Incarnation;
        DamagePlacement.Resolve(world, world.Facts, new(world.Seats[0].IdentityCard, loki, 6, "test", "test"), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(heals ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(0, loki.Damage);
        if (heals) Assert.Equal(incarnation, loki.Incarnation);
        if (!last) Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
        Assert.Equal(0, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    [Rule("rr:would.1")]
    public void HealingPreventsOverkillAndNoDefeatResponseIsOffered()
    {
        // RR 1.8: "If this attack defeats a minion, deal the damage beyond its hit points to the villain."
        // Healing in the would-defeat window prevents that required defeat.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect, Keywords.Overkill, Amount: 1, Affects: hero.ObjectId));
        DamageAttacks.Attack(world, world.Facts, hero, loki, 6, "test", "test", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, loki.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}



