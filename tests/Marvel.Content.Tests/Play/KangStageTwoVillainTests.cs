using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang II faces and the full authored book on a prepared two-area split;
// Kang's stage schemes and public admission remain outside this board.
public sealed class KangStageTwoVillainTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11002", "11009b")]
    [InlineData("11003", "11010b")]
    [InlineData("11004", "11011b")]
    [InlineData("11005", "11012b")]
    [InlineData("11035", "11009b")]
    [InlineData("11036", "11010b")]
    [InlineData("11037", "11011b")]
    [InlineData("11038", "11012b")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatRemovesTheMatchingLocalSchemeAndWaitsToJoin(string face, string schemeFace)
    {
        // 11003/11036: "When Defeated: Remove Inexorable Fate from the game. At the
        // end of the phase, join another game area." Immortus names the
        // Chronopolis, Rama-Tut the Realm of Rama-Tut and Scarlet Centurion The
        // Present Future War. "A defeated card leaves play after its When Defeated ability
        // is resolved." The other area's scheme stays.
        var (world, kang, scheme, other) = Board(face, schemeFace);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", []);
        Sequence.Finish(world, Cards, world.Abilities, []);

        var capture = Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(kang.ObjectId, capture.Defeated.Card);
        Assert.Equal(new[] { PlayArea.Of(0) }, capture.Group);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.Equal(DeckType.MainSchemesArea, other.Area.Type);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData("11005", "11012b", 3)]
    [InlineData("11038", "11012b", 4)]
    [InlineData("11003", "11010b", 0)]
    [InlineData("11036", "11010b", 0)]
    [Rule("rr:piercing.1")]
    [Rule("rr:tough.2")]
    public void OnlyScarletCenturionsAttackPiercesTough(string face, string schemeFace, int damage)
    {
        // 11005: "This villain's attack gains piercing." rr:piercing.1: "Before
        // this attack deals damage to a character, discard each tough status card
        // from that character." Iron Lad's attack is spent on the tough status.
        var (world, kang, _, _) = Board(face, schemeFace);
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Tough);
        KangTemporalFixture.Attack(world, kang, 0);

        Finish(world);

        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.Equal(damage, identity.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("11002", 0, false, 0, false)]
    [InlineData("11035", 0, true, 0, true)]
    [InlineData("11002", 1, false, 2, false)]
    [InlineData("11035", -1, false, 2, false)]
    [InlineData("11002", -1, true, 0, false)]
    [Rule("rr:cannot")]
    [Rule("rr:tough.2")]
    public void ImmortusCannotTakeDamageWhileAMinionIsInHisGameArea(
        string face, int minionSeat, bool tough, int damage, bool toughAfter)
    {
        // 11002/11035: "This villain cannot take damage while a minion is in
        // play." pack:mc11:game-areas: "Cards and components in one game area
        // cannot affect another game area", so a minion engaged with player 1, in
        // the other area, leaves him exposed (our reading). Ruling
        // 76a9465df95e739c (MARVEL-894 Q9): he keeps Tough while he cannot take
        // damage. Captain Marvel's basic attack deals 2; Shocker has no guard.
        var (world, kang, _, _) = Board(face, "11009b");
        if (minionSeat >= 0) KangTemporalFixture.Engaged(world, "01103", minionSeat);
        if (tough) Statuses.Give(world, kang, Statuses.Tough);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, kang, []);
        Finish(world);

        Assert.Equal(damage, kang.Damage);
        Assert.Equal(toughAfter, Statuses.Has(world, kang, Statuses.Tough));
    }

    [Theory]
    [InlineData("11004", false, 2 + 2)]
    [InlineData("11037", false, 3 + 2)]
    [InlineData("11004", true, 2 + 3)]
    [Rule("rr:obligation.5")]
    public void RamaTutGainsAttackForEachObligationInHisGameArea(string face, bool joined, int damage)
    {
        // 11004/11037: "This villain gets +1 ATK for each obligation in play."
        // MARVEL-894 Q4 counts the obligations in his game area. Player 0 holds
        // two and player 1 one; player 1's counts once both share the area.
        var (world, kang, _, _) = Board(face, "11011b", otherVillain: !joined);
        KangTemporalFixture.Obligations(world, 0, 2);
        KangTemporalFixture.Obligations(world, 1, 1);
        if (joined) world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared join", []);
        KangTemporalFixture.Attack(world, kang, 0);

        Finish(world);

        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(false, 2, false)]
    [Rule("rr:piercing.2")]
    public void ImmortusKeepsToughAgainstAPiercingAttackWhileProtected(bool minion, int damage, bool toughAfter)
    {
        // 04012: "Black Knight's basic attack gains piercing." rr:piercing.2: "If
        // an attack with the piercing keyword would deal no damage to the
        // attacked character, it does not discard tough status cards from that
        // character." Immortus cannot take damage while Shocker is engaged in
        // his game area, so the attack deals none and Tough stays (ruling
        // 76a9465df95e739c). Without Shocker the piercing discards it first.
        var (world, kang, _, _) = Board("11002", "11009b");
        if (minion) KangTemporalFixture.Engaged(world, "01103", 0);
        Statuses.Give(world, kang, Statuses.Tough);
        var knight = world.CreateCard("04012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        BasicPowers.AllyPower(world, world.Facts, knight, kang, BasicPowers.AttackVerb, []);
        Finish(world);

        Assert.Equal(damage, kang.Damage);
        Assert.Equal(toughAfter, Statuses.Has(world, kang, Statuses.Tough));
    }

    [Theory]
    [InlineData("11005", 0, true)]
    [InlineData("11038", 1, false)]
    [Rule("rr:piercing.2")]
    [Rule("rr:attack-enemy-activation.2.2")]
    public void ScarletCenturionKeepsOffToughWhenADefenceLeavesNoDamage(string face, int damage, bool toughAfter)
    {
        // rr:attack-enemy-activation.2.2: a defending hero with Tough reduces the
        // damage by DEF first; "If the damage is reduced to 0, the hero keeps
        // their tough status." rr:piercing.2 withholds the discard then. Spider-
        // Man's DEF 3 stops Scarlet Centurion's 3 but not the expert's 4.
        var (world, kang, _, _) = Board(face, "11012b", identity: "01001a,01001b");
        var spider = world.Seats[0].IdentityCard;
        Statuses.Give(world, spider, Statuses.Tough);
        KangTemporalFixture.Attack(world, kang, 0);
        int defences = 0;

        KangTemporalFixture.Drive(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities, prompt =>
        {
            if (prompt.Asking != Question.Defender) return null;
            var defence = prompt.Affordances.FirstOrDefault(option => option.Id == spider.ObjectId);
            if (defence is null) return Decision.Decline;
            defences++;
            return Decision.Take(defence.Id);
        });

        Assert.Equal(1, defences);
        Assert.Equal(damage, spider.Damage);
        Assert.Equal(toughAfter, Statuses.Has(world, spider, Statuses.Tough));
    }

    private static void Finish(World world) =>
        new RedSkullFixture(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities).Finish();

    internal static (World World, Card Kang, Card Scheme, Card Other) Board(
        string face, string schemeFace, bool otherVillain = true, string identity = "01010a,01010b")
    {
        var world = new World(Cards, 2, 42);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? identity : "01019a,01019b", seat.Hero);
            for (int i = 0; i < 6; i++) world.CreateCard("01088", seat.Deck);
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        world.Detach(PlayArea.Villains, "prepared split", []);
        var kang = world.CreateCard(face, world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var scheme = world.CreateCard(schemeFace, world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        if (otherVillain) world.CreateCard("11004", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        var other = world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)));
        // Hard to Keep Down prints no boost field, so every boost adds nothing.
        for (int i = 0; i < 6; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return (world, kang, scheme, other);
    }
}
