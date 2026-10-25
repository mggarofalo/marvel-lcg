using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingThunderballTests : CoreCardTestBase
{
    [Rule("rr:you-your.7")]
    [Rule("rr:defend-defense.5.1")]
    [Rule("rr:stun-stunned.7")]
    [Theory]
    [InlineData("07017", 1, -1, false)]
    [InlineData("07018", 2, -1, false)]
    [InlineData("07017", 1, 0, false)]
    [InlineData("07018", 2, 0, false)]
    [InlineData("07017", 1, 1, false)]
    [InlineData("07018", 2, 1, false)]
    [InlineData("07017", 1, -1, true)]
    [InlineData("07018", 2, -1, true)]
    public void AfterAttackDamagesOnlyCharactersControlledByTheFinalTargetPlayer(
        string face, int attack, int defenderSeat, bool stunned)
    {
        // 07017/07018: "After Thunderball attacks you, deal 1 damage to each character you control."
        // rr:you-your.7: "you" refers to "the attacked player, even if that player defended with an ally."
        // rr:defend-defense.5.1: after-attack "you" refers to "the player whose character defended the attack."
        // rr:stun-stunned.7: a replaced attack means the character "is not considered to have attacked."
        var world = Hero("01001a", players: 2);
        var second = world.Seats[1];
        World.MoveToTop(second.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        second.IdentityCard = world.CreateCard("01019a", second.Hero);
        foreach (var seat in world.Seats)
        {
            world.CreateCard("01088", seat.Deck);
            world.CreateCard("01089", seat.Deck);
        }
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var own = Group(world, face, "07019");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        Card[] allies = [Ally(world, "01002", 0), Ally(world, "01083", 0), Ally(world, "01051", 1)];
        if (stunned) Statuses.Give(world, own.Villain, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));

        Finish(world, runner, defenderSeat < 0 ? null : allies[defenderSeat == 0 ? 0 : 2]);

        AssertDamage(world, allies, attack, defenderSeat, stunned);
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.False(Statuses.Has(world, own.Villain, Statuses.Stunned));
    }

    private static void AssertDamage(World world, Card[] allies, int attack, int defenderSeat, bool stunned)
    {
        if (stunned)
        {
            Assert.All(world.Seats.Select(seat => seat.IdentityCard).Concat(allies), card => Assert.Equal(0, card.Damage));
            return;
        }
        Assert.Equal(defenderSeat < 0 ? attack + 1 : defenderSeat == 0 ? 1 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(defenderSeat == 1 ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        AssertAllyDamage(allies[0], defenderSeat == 1 ? 0 : defenderSeat == 0 ? 2 : 1, 2);
        AssertAllyDamage(allies[1], defenderSeat == 1 ? 0 : 1, 3);
        AssertAllyDamage(allies[2], defenderSeat == 1 ? attack + 1 : 0, 3);
    }

    [Theory]
    [InlineData("07017")]
    [InlineData("07018")]
    public void SchemeActivationTargetsThunderballsOwnScheme(string face)
    {
        // 07017/07018: "When Thunderball schemes, place the threat on his side scheme
        // instead of the main scheme."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var own = Group(world, face, "07019");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));

        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(3, own.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, other.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.Single(other.Deck.Cards);
        Assert.Equal(2, own.Deck.Cards.Count);
    }

    private static void AssertAllyDamage(Card ally, int damage, int hitPoints)
    {
        Assert.Equal(damage >= hitPoints ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        if (damage < hitPoints) Assert.Equal(damage, ally.Damage);
    }

    private static void Finish(World world, AbilityRunner runner, Card? defender)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events);
        for (int steps = 0; prompt is not null; steps++)
        {
            Assert.True(steps < 10);
            var answer = prompt.Asking == Question.Defender && defender is not null
                && prompt.Affordances.Any(option => option.Id == defender.ObjectId)
                ? Decision.Take(defender.ObjectId) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, answer, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }
    }

    private static Card Ally(World world, string face, int seat) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(seat), cardOwner: seat));

    private static EncounterDeckGroup Group(World world, string villain, string scheme)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        Card[] cards = [world.CreateCard("07037", deck), world.CreateCard("07037", deck)];
        return new(world.CreateCard(villain, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), cards);
    }
}
