using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CrossbonesMachineGunAttackTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your.7")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:attack-enemy-activation.step.1")]
    public void CrossbonesAttackSpendsAmmoAndDealsTheDiscardedBoostIcons(int attacked)
    {
        // 04064: "When Crossbones attacks you, remove 1 ammo counter from this card and
        // discard the top card of the encounter deck → take indirect damage equal to the
        // number of boost icons on the discarded card." rr:you-your.7: "For abilities that
        // trigger “after [enemy] attacks you,” “you” refers to the attacked player."
        // rr:interrupt.3: an interrupt "is resolved when its triggering condition initiates,
        // but before that triggering condition resolves", and the attack resolves from
        // rr:attack-enemy-activation step 1, "Give boost card". So the gun discards the top
        // card, Power Drain (02041, three boost icons), and the boost card is the next card,
        // Hard to Keep Down (01104, no boost icons).
        // A gun without ammo cannot pay its cost, so it is the control attack, whose boost
        // card is Power Drain.
        var (world, gun) = Board(ammo: 2, attacked);
        var (control, _) = Board(ammo: 0, attacked);
        int other = 1 - attacked;

        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041", "01104"], Faces(world));
        Assert.Equal(["02041"], Faces(control));
        // Crossbones' ATK 1 boosted by Power Drain's three icons.
        Assert.Equal(4, control.Seats[attacked].IdentityCard.Damage);
        // ATK 1 boosted by no icons, plus three indirect damage for the discarded Power Drain.
        Assert.Equal(1 + 0 + 3, world.Seats[attacked].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[other].IdentityCard.Damage);
        Assert.Equal(0, control.Seats[other].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:player-elimination.4")]
    public void LethalGunDamageToTheLastPlayerEndsTheGame()
    {
        // rr:player-elimination.4: "If all players are eliminated, the game ends and the
        // players lose." Power Drain's three icons exceed Captain Marvel's last hit point.
        var (world, gun) = Board(ammo: 2, attacked: 0, players: 1, remainingHealth: 1);

        Assert.True(world.Seats[0].Eliminated);
        Assert.True(world.IsOver);
        Assert.Equal(Outcome.PlayersLose, world.Result);
        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041"], Faces(world));
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.5.1")]
    public void LethalGunDamageToOnePlayerEndsTheAttackAndTheGameContinues()
    {
        // rr:player-elimination.5: "If a player is eliminated partway through the resolution
        // of an ability, resolve the entire ability." rr:player-elimination.5.1: "If a player
        // is eliminated in the middle of an attack, that attack ends." No boost card is dealt.
        var (world, gun) = Board(ammo: 2, attacked: 0, players: 2, remainingHealth: 1);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.IsOver);
        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041"], Faces(world));
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Null(world.Attack);
    }

    private static string[] Faces(World world) =>
        [.. world.AreaOf(DeckType.EncounterDiscardPile).Cards.Select(card => card.FaceId)];

    private static (World World, Card Gun) Board(int ammo, int attacked, int players = 2, int remainingHealth = 0)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01010a,01010b" : "01001a,01001b", seat.Hero);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        // Captain Marvel's printed 12 hit points.
        if (remainingHealth > 0) world.Seats[attacked].IdentityCard.TakeDamage(12 - remainingHealth);
        var crossbones = world.CreateCard("04058", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 6; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("02041", world.AreaOf(DeckType.EncounterDeck));
        var gun = world.CreateCard("04064", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, crossbones.ObjectId));
        if (ammo > 0) gun.PlaceTokens("c_ammo", ammo);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Index: attacked, Subject: crossbones.ObjectId, Seat: attacked));
        new RedSkullFixture(world, runner).Finish();
        return (world, gun);
    }
}
