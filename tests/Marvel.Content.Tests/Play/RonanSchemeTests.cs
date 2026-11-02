using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Ronan the Accuser's side schemes and treacheries over the production book.
public sealed class RonanSchemeTests : CoreCardTestBase
{
    private static Func<Prompt, Decision?> Option(string description) => prompt =>
        prompt.Affordances.FirstOrDefault(a => a.Description == description) is { } chosen ? Decision.Take(chosen.Id) : null;

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 2)]
    [Rule("rr:choose-option.1")]
    public void CutThePowerAsABoostExhaustsTheMilanoOrPlacesTwoThreat(bool ready, long threat)
    {
        // 16111: "Boost: Choose to either exhaust the Milano or place 2 threat on the main
        // scheme." An exhausted Milano leaves only the threat.
        var world = RonanBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        if (!ready) milano.Exhaust();
        BadoonBoards.OnTop(world, RonanBoards.CutThePower);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world, Option("Exhaust the Milano"));

        Assert.False(milano.Ready);
        Assert.Equal(threat, GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:hinder-x")]
    public void PincerManeuverEntersWithItsHinderAndYieldsToTheFirstPlayersAction()
    {
        // 16112: starting threat 3, "Hinder 2 per hero", "First Player Action: Exhaust the
        // Milano → remove 3 threat from this scheme."
        var world = RonanBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var scheme = RonanBoards.Reveal(world, RonanBoards.PincerManeuver);
        BadoonBoards.Drive(world);
        Assert.Equal(7, scheme.Tokens["k_threat"]);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, scheme, 1));
        ShipCommandBoards.Act(world, scheme, 0);

        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.False(milano.Ready);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [Rule("rr:attach-to.3.1")]
    public void SuperiorTacticsTakesThePowerStoneOrAddsThreatWhenRonanHoldsIt(bool ronanHolds, long threat)
    {
        // 16113: "When Revealed: Attach the Power Stone to Ronan the Accuser. If it is already
        // attached to him, place 1 per hero threat here." It starts with 2 per hero threat.
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var stone = RonanBoards.Stone(world, ronanHolds ? ronan : world.Seats[0].IdentityCard);
        var scheme = RonanBoards.Reveal(world, RonanBoards.SuperiorTactics);

        BadoonBoards.Drive(world);

        Assert.Equal(ronan, RonanBoards.Host(world, stone));
        Assert.Equal(threat, scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:cannot")]
    public void SuperiorTacticsKeepsThePowerStoneOnRonan(bool tactics)
    {
        // 16113: "The Power Stone cannot be unattached from Ronan the Accuser." She-Hulk's basic
        // attack deals 3 damage, which otherwise takes the stone (16149).
        var world = ShipCommandBoards.PowerStoneBoard("01019a,01019b", villain: RonanBoards.RonanOne);
        var ronan = GrootBoards.Villain(world);
        var stone = RonanBoards.Stone(world, ronan);
        if (tactics) GrootBoards.Side(world, RonanBoards.SuperiorTactics, 2);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, ronan, []);
        BadoonBoards.Drive(world);

        Assert.Equal(3, ronan.Damage);
        Assert.Equal(tactics ? ronan : world.Seats[0].IdentityCard, RonanBoards.Host(world, stone));
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.1.1")]
    public void SingleMindedFuryAttacksTheAlterEgoWhoControlsThePowerStone()
    {
        // 16114: "When Revealed: Ronan the Accuser attacks the player who controls the Power
        // Stone (even if that player is in alter-ego form)." Two boost cards, one for the stone.
        var world = RonanBoards.Board(face: "01001b");
        RonanBoards.Stone(world, world.Seats[0].IdentityCard);
        RonanBoards.Reveal(world, RonanBoards.SingleMindedFury);
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        BadoonBoards.Drive(world);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(deck - 2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.1.1")]
    public void SingleMindedFuryAttacksTheOtherPlayerWhenTheyControlTheStone()
    {
        var world = RonanBoards.Board(players: 2);
        RonanBoards.Stone(world, world.Seats[1].IdentityCard);
        RonanBoards.Reveal(world, RonanBoards.SingleMindedFury);

        BadoonBoards.Drive(world);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation.7")]
    public void SingleMindedFurySurgesWhenNoAttackIsMade(bool stunned)
    {
        // "If no attack was made this way, this card gains surge." No player controls a stone on
        // Ronan; a stunned Ronan's attack is replaced by removing the stun. The surge deals
        // Pincer Maneuver from the top of the encounter deck for this step to reveal.
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        RonanBoards.Stone(world, stunned ? world.Seats[0].IdentityCard : ronan);
        if (stunned) Statuses.Give(world, ronan, Statuses.Stunned);
        var pincer = BadoonBoards.OnTop(world, RonanBoards.PincerManeuver);
        RonanBoards.Reveal(world, RonanBoards.SingleMindedFury);

        BadoonBoards.Drive(world);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, pincer.Area.Type);
        Assert.False(Statuses.Has(world, ronan, Statuses.Stunned));
    }

    [Fact]
    [Rule("rr:attach-to.3.1")]
    public void SingleMindedFuryAsABoostTakesThePowerStone()
    {
        // 16114: "Boost: Attach the Power Stone to Ronan the Accuser."
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var stone = RonanBoards.Stone(world, world.Seats[0].IdentityCard);
        BadoonBoards.OnTop(world, RonanBoards.SingleMindedFury);

        KangTemporalFixture.Activate(world, ronan, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(ronan, RonanBoards.Host(world, stone));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [Rule("rr:status-cards.1")]
    public void KreePhysiologyToughensRonanOrDamagesYouIfHeIsAlreadyTough(bool tough, long damage)
    {
        // 16115: "Surge. When Revealed: Give Ronan the Accuser a tough status card. If he already
        // has a tough status card, take 1 damage." The surge deals Pincer Maneuver to be revealed.
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        if (tough) Statuses.Give(world, ronan, Statuses.Tough);
        var pincer = BadoonBoards.OnTop(world, RonanBoards.PincerManeuver);
        RonanBoards.Reveal(world, RonanBoards.KreePhysiology);

        BadoonBoards.Drive(world);

        Assert.True(Statuses.Has(world, ronan, Statuses.Tough));
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, pincer.Area.Type);
    }

    [Theory]
    [InlineData("01001a", 3, 0)]
    [InlineData("01001b", 0, 3)]
    [Rule("rr:when-revealed-abilities")]
    public void YouStandAccusedAttacksTheHeroOrSchemesAgainstTheAlterEgo(string face, long damage, long threat)
    {
        // 16116: "When Revealed (Alter-Ego): Ronan the Accuser schemes with +1 SCH. When Revealed
        // (Hero): Ronan the Accuser attacks you with +1 ATK." Ronan (I) has 2 ATK and 2 SCH.
        var world = RonanBoards.Board(face: face);
        RonanBoards.Reveal(world, RonanBoards.YouStandAccused);

        BadoonBoards.Drive(world);

        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat, GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    public void YouStandAccusedAsABoostGivesRonanAnotherBoostCard()
    {
        // 16116: "Boost: Give the villain 1 additional boost card for this activation." It
        // prints one boost icon; the additional Hard to Keep Down prints none.
        var world = RonanBoards.Board();
        BadoonBoards.OnTop(world, RonanBoards.YouStandAccused);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(2, RonanBoards.Discarded(world));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }
}
