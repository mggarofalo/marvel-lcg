using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang II faces and the full authored book on a prepared two-area split;
// Kang's stage schemes and public admission remain outside this board.
public sealed class KangStageTwoVillainTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11003", "11010b")]
    [InlineData("11005", "11012b")]
    [InlineData("11036", "11010b")]
    [InlineData("11038", "11012b")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatRemovesTheMatchingLocalSchemeAndWaitsToJoin(string face, string schemeFace)
    {
        // 11003/11036: "When Defeated: Remove Inexorable Fate from the game. At the
        // end of the phase, join another game area." 11005/11038 name The Present
        // Future War. "A defeated card leaves play after its When Defeated ability
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

        new RedSkullFixture(world, (Marvel.Cards.Run.AbilityRunner)world.Abilities).Finish();

        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.Equal(damage, identity.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    internal static (World World, Card Kang, Card Scheme, Card Other) Board(string face, string schemeFace)
    {
        var world = new World(Cards, 2, 42);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01010a,01010b" : "01019a,01019b", seat.Hero);
            for (int i = 0; i < 6; i++) world.CreateCard("01088", seat.Deck);
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        world.Detach(PlayArea.Villains, "prepared split", []);
        var kang = world.CreateCard(face, world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var scheme = world.CreateCard(schemeFace, world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        world.CreateCard("11004", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        var other = world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)));
        // Hard to Keep Down prints no boost field, so every boost adds nothing.
        for (int i = 0; i < 6; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return (world, kang, scheme, other);
    }
}
