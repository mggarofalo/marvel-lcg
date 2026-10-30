using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Temporal Shield (11014) and the full authored book on a nonpublic Rhino board,
// where Rhino is the villain in the only game area; Kang's public admission stays closed.
public sealed class KangTemporalShieldTests : CoreCardTestBase
{
    private const string TemporalShield = "11014";

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:damage.step.1")]
    [Rule("rr:tough.2")]
    public void TheShieldPreventsTheHeroAttackAndHitsTheAttacker(int attacker, bool tough)
    {
        // 11014: "When Kang is attacked, discard Temporal Shield → prevent all damage
        // from this attack and deal 1 damage to the attacker." Ruling a806907d1743e0d1:
        // the shield "resolves entirely before the tough status card, meaning the tough
        // status card will have no damage to prevent and will not be discarded."
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var shield = Attach(f, player: 1 - attacker);
        if (tough) Statuses.Give(world, f.Villain, Statuses.Tough);
        var hero = world.Seats[attacker].IdentityCard;

        BasicPowerInitiation.BasicAttack(world, world.Facts, attacker, f.Villain, []);
        f.Finish();

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(tough, Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.Equal(1, hero.Damage);
        Assert.Equal(0, world.Seats[1 - attacker].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, shield.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:damage.step.1")]
    public void OnlyTheShieldedAttackIsPrevented()
    {
        // The shield is discarded as its cost, so Captain Marvel's (ATK 2) next
        // attack on Rhino deals its damage normally.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        Attach(f);
        var hero = world.Seats[0].IdentityCard;

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, f.Villain, []);
        f.Finish();
        BasicPowerInitiation.BasicAttackWithoutExhausting(world, world.Facts, hero, f.Villain, []);
        f.Finish();

        Assert.Equal(2, f.Villain.Damage);
        Assert.Equal(1, hero.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void AnAttackingAllyTakesTheShieldsDamage()
    {
        // "Deal 1 damage to the attacker": Jessica Jones (ATK 2) attacks, so she, not
        // the identity, is dealt the 1. An unshielded control fixes her own
        // consequential damage.
        var (control, controlAlly) = AllyAttack(shielded: false);
        var (world, ally) = AllyAttack(shielded: true);

        Assert.Equal(2, control.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(controlAlly.Damage + 1, ally.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void AnAttackEventIsPreventedAndItsHeroTakesTheDamage()
    {
        // Uppercut (01054): "Hero Action (attack): Deal 5 damage to an enemy." The
        // event's attack is Spider-Man's (rr:you-your.12), so he is the attacker.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var shield = Attach(f);
        var hand = world.Seats[1].Hand;
        var uppercut = world.CreateCard("01054", hand);
        var genius = world.CreateCard("01089", hand);
        var energy = world.CreateCard("01088", hand);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == uppercut.ObjectId);

        f.Runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
        f.Finish(prompt => Decision.Take(f.Villain.ObjectId));

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, shield.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void AnAttackOnAMinionLeavesTheShieldInPlay()
    {
        // "When Kang is attacked": Captain Marvel attacks an engaged Shocker instead.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var shield = Attach(f);
        var shocker = KangTemporalFixture.Engaged(world, "01103", 0);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, shocker, []);
        f.Finish();

        Assert.Equal(2, shocker.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(f.Villain.ObjectId, shield.Area.Host);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:max-maximum")]
    public void OnlyOneShieldResolvesForAnAttack()
    {
        // "(Max 1 per attack.)" Two shields on Rhino: one is discarded, the attacker
        // takes 1 damage, and the other stays attached for a later attack.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var first = Attach(f);
        var second = Attach(f);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, f.Villain, []);
        f.Finish(prompt => Decision.Take(prompt.Affordances[0].Id));

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(new[] { first, second }, card => card.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Single(new[] { first, second }, card => card.Area.Host == f.Villain.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, Card Ally) AllyAttack(bool shielded)
    {
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        if (shielded) Attach(f, player: 1);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, f.Villain, BasicPowers.AttackVerb, []);
        f.Finish();
        return (world, ally);
    }

    private static Card Attach(RedSkullFixture f, int player = 0)
    {
        var shield = f.Reveal(TemporalShield, player);
        f.Finish();
        Assert.Equal(f.Villain.ObjectId, shield.Area.Host);
        return shield;
    }
}
