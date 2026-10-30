using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Future Weapon (11015) and Macrobots (11017) with the full authored book on a
// nonpublic Rhino board, where Rhino (ATK 2) is the villain in the only game area;
// the encounter deck's Hard to Keep Down prints no boost icons. Kang's public
// admission stays closed.
public sealed class KangFutureWeaponTests : CoreCardTestBase
{
    private const string FutureWeapon = "11015";
    private const string Macrobots = "11017";

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:star-icon.2")]
    [Rule("rr:delayed-effect.1")]
    public void AnUndefendedAttackDamagesAndStunsTheHeroThenDiscardsTheWeapon(int player, bool armed)
    {
        // 11015: "When Kang attacks, the attack gains overkill. If this attack damages
        // a hero, that hero is stunned. After this attack, discard Future Weapon." Its
        // printed ATK+ 2 makes Rhino's attack 4; without it the attack is 2 and
        // nobody is stunned.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var weapon = armed ? Attach(f) : null;
        var hero = world.Seats[player].IdentityCard;
        KangTemporalFixture.Attack(world, f.Villain, player);

        f.Finish();

        Assert.Equal(armed ? 4 : 2, hero.Damage);
        Assert.Equal(armed, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1 - player].IdentityCard, Statuses.Stunned));
        if (weapon is not null) Assert.Equal(DeckType.EncounterDiscardPile, weapon.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void AToughHeroIsNotDamagedAndSoNotStunned()
    {
        // rr:tough.3: "the character who had the tough status card is not considered
        // to have taken damage." The weapon is still discarded after the attack.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var weapon = Attach(f);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);
        KangTemporalFixture.Attack(world, f.Villain, 0);

        f.Finish();

        Assert.Equal(0, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(DeckType.EncounterDiscardPile, weapon.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    public void OverkillThroughADefendingAllyStunsTheHeroButNotTheAlly()
    {
        // Jessica Jones (HP 3) defends Spider-Man against the 4-ATK attack. Overkill
        // carries 1 to Spider-Man, who is "a hero" damaged by this attack; the ally
        // is not a hero.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        Attach(f);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var hero = world.Seats[1].IdentityCard;
        KangTemporalFixture.Attack(world, f.Villain, 1);

        KangTemporalFixture.Drive(world, f.Runner, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense" && a.AnchorId == ally.ObjectId) is { } defense
                ? Decision.Take(defense.Id) : null);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(1, hero.Damage);
        Assert.True(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:defend-defense.1")]
    public void ADefendingHeroOfAnotherPlayerIsTheStunnedHero()
    {
        // Spider-Man (DEF 3) defends Captain Marvel: 4 - 3 = 1 damage lands on
        // Spider-Man, who is stunned; Captain Marvel was not damaged.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        Attach(f);
        var attacked = world.Seats[0].IdentityCard;
        var defender = world.Seats[1].IdentityCard;
        KangTemporalFixture.Attack(world, f.Villain, 0);

        KangTemporalFixture.Drive(world, f.Runner, prompt => prompt.Player == 1
            && prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense
            ? Decision.Take(defense.Id) : null);

        Assert.Equal(1, defender.Damage);
        Assert.True(Statuses.Has(world, defender, Statuses.Stunned));
        Assert.Equal(0, attacked.Damage);
        Assert.False(Statuses.Has(world, attacked, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attachment.1")]
    public void TheWeaponArmsOnlyOneAttack()
    {
        // "After this attack, discard Future Weapon": a second attack is Rhino's own 2.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        Attach(f);
        var hero = world.Seats[0].IdentityCard;
        KangTemporalFixture.Attack(world, f.Villain, 0);
        f.Finish();
        KangTemporalFixture.Attack(world, f.Villain, 0);

        f.Finish();

        Assert.Equal(4 + 2, hero.Damage);
        Assert.Single(Statuses.On(world, hero, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:star-icon.6")]
    public void MacrobotsBoostGivesTheVillainTough(int player)
    {
        // 11017: "Boost: Give Kang a tough status card." Its boost field is a star
        // with no icons, so the attack stays at 2.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        world.CreateCard(Macrobots, world.AreaOf(DeckType.EncounterDeck));
        var hero = world.Seats[player].IdentityCard;
        KangTemporalFixture.Attack(world, f.Villain, player);

        f.Finish();

        Assert.True(Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.Equal(2, hero.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void MacrobotsBoostOnAMinionsActivationStillToughensTheVillain()
    {
        // "Kang", not "this enemy": Kang (Master of Time) is villainous, so Macrobots as
        // his boost card gives the villain (Rhino here) tough and not the minion.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.CreateCard(Macrobots, world.AreaOf(DeckType.EncounterDeck));
        var minion = KangTemporalFixture.Engaged(world, "11047", 1);
        KangTemporalFixture.Attack(world, minion, 1);

        var events = f.Finish();

        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == minion.ObjectId);
        Assert.True(Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Attach(RedSkullFixture f, int player = 0)
    {
        var weapon = f.Reveal(FutureWeapon, player);
        f.Finish();
        Assert.Equal(f.Villain.ObjectId, weapon.Area.Host);
        return weapon;
    }
}
