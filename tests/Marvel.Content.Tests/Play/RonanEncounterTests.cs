using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The Kree Command Ship, the Universal Weapon and Fanaticism over the production book.
public sealed class RonanEncounterTests : CoreCardTestBase
{
    private const string Interrupt = "First Player Interrupt";

    private static bool Offered(IEnumerable<Prompt> asked, int player) => asked.Any(prompt => prompt.Player == player
        && prompt.Affordances.Any(a => a.Label == Interrupt));

    [Fact]
    [Rule("rr:cancel.4")]
    public void TheFirstPlayerCancelsATreacheryAnotherPlayerReveals()
    {
        // 16108: "First Player Interrupt: When a treachery card is revealed from the encounter
        // deck, exhaust the Milano and spend 1 resource of any type → cancel that card's 'When
        // Revealed' effects." Player 1 reveals "You Stand Accused!" in hero form.
        var world = RonanBoards.Board(players: 2);
        RonanBoards.Ship(world);
        var milano = ShipCommandBoards.PutMilano(world);
        var energy = GrootBoards.Hand(world, ShipCommandBoards.Energy);
        var card = RonanBoards.Reveal(world, RonanBoards.YouStandAccused, 1);

        var asked = RonanBoards.Take(world, Interrupt, energy);

        Assert.False(Offered(asked, 1));
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.False(milano.Ready);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }

    [Fact]
    [Rule("rr:first-player.4")]
    public void OnlyTheFirstPlayerIsOfferedTheKreeCommandShip()
    {
        // Both players could pay; the first player declines and player 1 is never asked.
        var world = RonanBoards.Board(players: 2);
        RonanBoards.Ship(world);
        ShipCommandBoards.PutMilano(world);
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        GrootBoards.Hand(world, ShipCommandBoards.Energy, 1);
        RonanBoards.Reveal(world, RonanBoards.YouStandAccused, 1);

        var asked = RonanBoards.Take(world, null);

        Assert.True(Offered(asked, 0));
        Assert.False(Offered(asked, 1));
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [Rule("rr:peril.1")]
    public void TheFirstPlayerCannotCancelAnotherPlayersPerilTreachery(int revealer, bool offered)
    {
        // 16146 Hull Breach is a peril treachery: while its revealer resolves it, other players
        // cannot trigger abilities. The first player may still cancel their own.
        var world = RonanBoards.Board(players: 2);
        RonanBoards.Ship(world);
        ShipCommandBoards.PutMilano(world);
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        RonanBoards.Reveal(world, "16146", revealer);

        var asked = RonanBoards.Take(world, null);

        Assert.Equal(offered, Offered(asked, 0));
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void TheKreeCommandShipIsNotOfferedWithoutAReadyMilano()
    {
        // Ronan the Accuser (I) attacks with 2 ATK + 1.
        var world = RonanBoards.Board();
        RonanBoards.Ship(world);
        ShipCommandBoards.PutMilano(world).Exhaust();
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        RonanBoards.Reveal(world, RonanBoards.YouStandAccused);

        var asked = RonanBoards.Take(world, Interrupt);

        Assert.False(Offered(asked, 0));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:cancel.4")]
    public void TheKreeCommandShipIgnoresACardThatIsNotATreachery()
    {
        var world = RonanBoards.Board();
        RonanBoards.Ship(world);
        ShipCommandBoards.PutMilano(world);
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        // Superior Tactics is a side scheme with a When Revealed ability.
        var stone = RonanBoards.Stone(world, world.Seats[0].IdentityCard);
        var scheme = RonanBoards.Reveal(world, RonanBoards.SuperiorTactics);

        var asked = RonanBoards.Take(world, Interrupt);

        Assert.False(Offered(asked, 0));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(GrootBoards.Villain(world), RonanBoards.Host(world, stone));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:surge")]
    public void CancelingKreePhysiologyAlsoCancelsItsSurge(bool cancel)
    {
        // Ruling 4e44bf466c0d8971: surge is a "When Revealed" ability, so canceling the When
        // Revealed effects cancels it too. 16115: "Surge. When Revealed: Give Ronan the Accuser
        // a tough status card."
        var world = RonanBoards.Board();
        RonanBoards.Ship(world);
        ShipCommandBoards.PutMilano(world);
        var energy = GrootBoards.Hand(world, ShipCommandBoards.Energy);
        RonanBoards.Reveal(world, RonanBoards.KreePhysiology);
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        var asked = RonanBoards.Take(world, cancel ? Interrupt : null, energy);

        Assert.Equal(!cancel, Statuses.Has(world, GrootBoards.Villain(world), Statuses.Tough));
        Assert.Equal(cancel, world.AreaOf(DeckType.EncounterDeck).Cards.Count == deck);
        Assert.True(Offered(asked, 0));
    }

    [Fact]
    [Rule("rr:stalwart")]
    public void TheUniversalWeaponGivesRonanStalwartAndOneAttackAndScheme()
    {
        // 16109: ATK+1, SCH+1. "Ronan the Accuser gains stalwart."
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, ronan, RonanBoards.UniversalWeapon);

        Assert.Equal(1, RonanBoards.Modified(world, ronan, "stalwart"));
        Assert.Equal(3, RonanBoards.Modified(world, ronan, "attack"));
        Assert.Equal(3, RonanBoards.Modified(world, ronan, "scheme"));
    }

    [Fact]
    [Rule("rr:cost")]
    public void AHeroTakesTwoDamageAndAnEncounterCardToShuffleTheUniversalWeaponAway()
    {
        // 16109: "Hero Action: Take 2 damage and deal yourself 1 facedown encounter card →
        // shuffle Universal Weapon into the encounter deck."
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var weapon = ShipCommandBoards.Attach(world, ronan, RonanBoards.UniversalWeapon);

        ShipCommandBoards.Act(world, weapon, 0);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDeck, weapon.Area.Type);
        Assert.Equal(0, RonanBoards.Modified(world, ronan, "stalwart"));
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    public void TheUniversalWeaponAsABoostCardAttachesToRonan()
    {
        // 16109: "Boost: Attach Universal Weapon to Ronan the Accuser." It prints no boost icon,
        // and the attached weapon's ATK+1 is already on Ronan when the attack deals its damage.
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var weapon = BadoonBoards.OnTop(world, RonanBoards.UniversalWeapon);

        KangTemporalFixture.Activate(world, ronan, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(ronan.ObjectId, weapon.Area.Host);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, RonanBoards.Modified(world, ronan, "attack"));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [Rule("rr:uses-x-type.1")]
    public void FanaticismEntersWithOneFuryCounterPlusOnePerPlayer(int players, long counters)
    {
        // 16110: "Surge. Attach to Ronan the Accuser. Uses (1 fury counter, plus 1 per hero
        // additional fury counters)."
        var world = RonanBoards.Board(players);
        var fanaticism = RonanBoards.Reveal(world, RonanBoards.Fanaticism);

        ShipCommandBoards.Choose(world);

        Assert.Equal(GrootBoards.Villain(world).ObjectId, fanaticism.Area.Host);
        Assert.Equal(counters, fanaticism.Tokens.GetValueOrDefault("c_fury"));
        Assert.Equal(3, RonanBoards.Modified(world, GrootBoards.Villain(world), "attack"));
    }

    [Fact]
    [Rule("rr:piercing")]
    public void FanaticismGivesRonansAttackPiercingAndSpendsAFuryCounter()
    {
        // 16110: "Forced Interrupt: When Ronan the Accuser attacks you, that attack gains
        // overkill and piercing. At the end of that attack, remove 1 fury counter from here."
        var world = RonanBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var ronan = GrootBoards.Villain(world);
        var fanaticism = ShipCommandBoards.Attach(world, ronan, RonanBoards.Fanaticism);
        fanaticism.PlaceTokens("c_fury", 2);
        Statuses.Give(world, hero, Statuses.Tough);

        KangTemporalFixture.Activate(world, ronan, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(3, hero.Damage);
        Assert.Equal(1, fanaticism.Tokens.GetValueOrDefault("c_fury"));
        Assert.Equal(0, RonanBoards.Modified(world, ronan, "piercing"));
    }

    [Fact]
    [Rule("rr:overkill")]
    public void FanaticismGivesRonansAttackOverkillAndLeavesWithItsLastCounter()
    {
        // Ronan attacks for 3; Black Cat (2 hit points) defends and the excess 1 damage is
        // dealt to her controller's identity. The last fury counter discards Fanaticism.
        var world = RonanBoards.Board();
        var hero = world.Seats[0].IdentityCard;
        var ronan = GrootBoards.Villain(world);
        var fanaticism = ShipCommandBoards.Attach(world, ronan, RonanBoards.Fanaticism);
        fanaticism.PlaceTokens("c_fury", 1);
        var cat = GrootBoards.InPlay(world, "01002", DeckType.AlliesArea);

        KangTemporalFixture.Activate(world, ronan, scheme: false, 0);
        BadoonBoards.Drive(world, prompt => prompt.Asking == Question.Defender ? Decision.Take(cat.ObjectId) : null);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, fanaticism.Area.Type);
    }
}
