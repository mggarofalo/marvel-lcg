using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Anachronauts minion faces and the full authored book on a nonpublic
// Rhino board; Kang's public admission stays closed.
public sealed class KangAnachronautsMinionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11041", false, 1)]
    [InlineData("11041", true, 1)]
    [InlineData("11042", false, 1)]
    [InlineData("11042", true, 1)]
    [InlineData("11043", false, 2)]
    [InlineData("11043", true, 2)]
    [InlineData("11044", false, 1)]
    [InlineData("11044", true, 1)]
    [InlineData("01104", false, 0)]
    [InlineData("01104", true, 0)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:boost-boost-icon.4")]
    [Rule("rr:you-your.2")]
    [Rule("rr:you-your.8")]
    public void AnachronautBoostsResolveAgainstThePlayerTheVillainActivatesAgainst(
        string boost, bool scheme, int extra)
    {
        // rr:boost-boost-icon: "resolve the 'Boost' ability when the card is turned
        // face up." rr:boost-boost-icon.4: "If additional boost cards are resolved
        // for an activation, the boost icons are cumulative." 11041 gives "this enemy
        // a tough status card and another boost card"; 11042 "Take 1 damage"; 11043
        // "2 more boost cards"; 11044 "Discard 1 random card from your hand". Each
        // Hydra Mercenary beneath prints one boost icon. Player 1 is not the first player.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        for (int i = 0; i < 3; i++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard(boost, world.AreaOf(DeckType.EncounterDeck));
        for (int i = 0; i < 2; i++)
        {
            world.CreateCard("01088", world.Seats[0].Hand);
            world.CreateCard("01088", world.Seats[1].Hand);
        }
        KangTemporalFixture.Activate(world, f.Villain, scheme, 1);

        var events = f.Finish();

        int raston = boost == "11042" ? 1 : 0;
        Assert.Equal((scheme ? 0 : 2 + extra) + raston, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(scheme ? 1 + extra : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(boost == "11041", Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.Equal(boost == "11044" ? 1 : 2, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1 + extra, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
        Assert.Equal(3 - extra, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("11041", 3, 1)]
    [InlineData("01101", 1, 3)]
    [Rule("rr:villainous")]
    [Rule("rr:villainous.1")]
    public void DeathuntIsBoostedAndABoostsThisEnemyIsTheActivatingMinion(
        string enemy, int damage, int deck)
    {
        // rr:villainous.1: "Forced Interrupt: When this character uses a basic
        // power, give it a boost card." Sir Raston's boost deals 1 damage and gives
        // "this enemy another boost card", which is the activating minion, not the
        // villain. Hydra Mercenary has no villainous keyword and draws no boost card.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("11042", world.AreaOf(DeckType.EncounterDeck));
        var attacker = KangTemporalFixture.Engaged(world, enemy, 1);
        KangTemporalFixture.Attack(world, attacker, 1);

        var events = f.Finish();

        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(deck, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == attacker.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("11043", 2)]
    [InlineData("01167", 0)]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:piercing.1")]
    [Rule("rr:tough.1")]
    public void RevealedTerminatrixQuickstrikesThroughATough(string minion, int damage)
    {
        // rr:quickstrike.1: "After this minion engages a player, it attacks that
        // player." 11043: "Terminatrix's attacks gain piercing." rr:piercing.1:
        // "Before this attack deals damage to a character, discard each tough status
        // card from that character." Vulture quickstrikes without piercing, so
        // rr:tough.1 spends the tough card instead of the damage.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var identity = world.Seats[1].IdentityCard;
        Statuses.Give(world, identity, Statuses.Tough);
        var revealed = f.Reveal(minion, player: 1);

        var events = f.Finish();

        Assert.Equal(DeckType.EngagedEnemiesArea, revealed.Area.Type);
        Assert.Equal(PlayArea.Of(1), revealed.Area.PlayArea);
        Assert.Equal(damage > 0, Keywords.Has(world, revealed, "piercing", Cards));
        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.Equal(damage, identity.Damage);
        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == revealed.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("11041", "toughness")]
    [InlineData("11041", "villainous")]
    [InlineData("11042", "guard")]
    [InlineData("11042", "retaliate")]
    [InlineData("11043", "quickstrike")]
    [Rule("rr:guard")]
    [Rule("rr:retaliate-x")]
    public void AnachronautPrintedKeywordsAreTheEnginesToHold(string face, string keyword)
    {
        // Each printed keyword reaches the field the engine's keyword rules read.
        // rr:guard: "that player cannot use cards they control to attack a villain
        // without this keyword." rr:retaliate-x: "After a character with the
        // retaliate X keyword is attacked, deal X damage to the attacker."
        var (world, _) = KangTemporalFixture.Board(players: 1);
        var minion = KangTemporalFixture.Engaged(world, face, 0);

        Assert.Equal(1, Modified(world, minion, keyword));
    }

    [Fact]
    [Rule("rr:toughness.1")]
    public void RevealedDeathuntEntersWithATough()
    {
        // rr:toughness.1: "Forced Response: After this character enters play, give
        // it a tough status card." Its boost text is inactive on a revealed card.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var deathunt = f.Reveal("11041");

        f.Finish();

        Assert.Equal(DeckType.EngagedEnemiesArea, deathunt.Area.Type);
        Assert.Single(Statuses.On(world, deathunt, Statuses.Tough));
        Assert.False(Statuses.Has(world, f.Villain, Statuses.Tough));
        Assert.Equal(6, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }
}
