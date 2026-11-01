using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The production Ship Command treacheries on synthetic boards.
public sealed class ShipCommandTreacheryTests : CoreCardTestBase
{
    private const string BlindSide = "16145";
    private const string HullBreach = "16146";
    private const string PowerSiphon = "16147";
    private const string SpecialDelivery = "16148";

    [Fact]
    [Rule("rr:choose-option.1")]
    public void BlindSideMayExhaustTheMilano()
    {
        // 16145: "Choose one: -Exhaust the Milano. -Spend [physical][physical] resources.
        // - Stun the first player." With nothing to spend, the spend option is not offered.
        var world = ShipCommandBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        var card = ShipCommandBoards.Reveal(world, BlindSide);

        var asked = ShipCommandBoards.Choose(world, "Exhaust the Milano");

        Assert.Equal(["effect"], asked[0].Affordances.Select(a => a.Label));
        Assert.Equal(2, asked[1].Affordances.Count);
        Assert.False(milano.Ready);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void BlindSideStunsTheFirstPlayerWhenTheMilanoIsAlreadyExhausted()
    {
        // An exhausted Milano is not a valid target, so only the stun remains (rr:choose-option.1).
        var world = ShipCommandBoards.Board();
        ShipCommandBoards.PutMilano(world).Exhaust();
        ShipCommandBoards.Reveal(world, BlindSide);

        var asked = ShipCommandBoards.Choose(world);

        Assert.Equal("Stun the first player", Assert.Single(asked[^1].Affordances).Description);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void BlindSideOffersToSpendTwoPhysicalResourcesWhenTheyCanBePaid()
    {
        var world = ShipCommandBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        var first = GrootBoards.Hand(world, ShipCommandBoards.Strength);
        var second = GrootBoards.Hand(world, ShipCommandBoards.Strength);
        ShipCommandBoards.Reveal(world, BlindSide);

        ShipCommandBoards.Choose(world, paying: [first, second]);

        Assert.True(milano.Ready);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Fact]
    [Rule("rr:first-player")]
    public void HullBreachDealsThreeDamageToTheFirstPlayerNotTheRevealer()
    {
        // 16146: "- Deal 3 damage to the first player." Player 1 reveals; player 0 is first.
        var world = ShipCommandBoards.Board(players: 2);
        ShipCommandBoards.PutMilano(world).Exhaust();
        ShipCommandBoards.Reveal(world, HullBreach, player: 1);

        var asked = ShipCommandBoards.Choose(world);

        Assert.All(asked, prompt => Assert.Equal(1, prompt.Player));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:peril.1")]
    public void HullBreachDamageToTheFirstPlayerOffersThemNoInterrupt(bool peril)
    {
        // Ruling 1a4413236d2a28d8: while a peril card resolves, the first player cannot
        // trigger abilities against its damage. Energy Barrier (05017) is an optional
        // interrupt to any damage to its controller; Rogue Vessel's damage, outside peril,
        // offers it.
        var world = ShipCommandBoards.Board(players: 2);
        var barrier = GrootBoards.InPlay(world, "05017", DeckType.UpgradesArea);
        barrier.PlaceTokens("c_reflection", 3);
        ShipCommandBoards.PutMilano(world).Exhaust();
        if (peril) ShipCommandBoards.Reveal(world, HullBreach, player: 1);
        else
        {
            world.CreateCard("16143", world.AreaOf(DeckType.EnvironmentArea));
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
        }

        var asked = ShipCommandBoards.Choose(world);

        Assert.Equal(!peril, asked.Any(prompt => prompt.Affordances.Any(a => a.AnchorId == barrier.ObjectId)));
        Assert.Equal(peril ? 3 : 1, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:first-player")]
    public void PowerSiphonDiscardsAtRandomFromTheFirstPlayersHand()
    {
        // 16147: "- Discard 1 card at random from the first player's hand."
        var world = ShipCommandBoards.Board(players: 2);
        ShipCommandBoards.PutMilano(world).Exhaust();
        var held = GrootBoards.Hand(world, ShipCommandBoards.Genius, player: 0);
        var revealerCard = GrootBoards.Hand(world, ShipCommandBoards.Genius, player: 1);
        ShipCommandBoards.Reveal(world, PowerSiphon, player: 1);

        ShipCommandBoards.Choose(world);

        Assert.Equal(DeckType.DiscardPile, held.Area.Type);
        Assert.Equal(DeckType.HandsArea, revealerCard.Area.Type);
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void SpecialDeliveryInHeroFormAvoidsTheAttackByExhaustingTheMilano()
    {
        // 16148: "When Revealed (Hero): You may exhaust the Milano. If you do not, the
        // villain attacks you with +1 ATK."
        var world = ShipCommandBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        ShipCommandBoards.Reveal(world, SpecialDelivery);

        ShipCommandBoards.Choose(world, "Exhaust the Milano");

        Assert.False(milano.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void SpecialDeliveryInHeroFormAttacksWithPlusOneWhenTheMilanoIsExhausted()
    {
        // Rhino (I) has 2 ATK; +1 for this activation, undefended.
        var world = ShipCommandBoards.Board();
        ShipCommandBoards.PutMilano(world).Exhaust();
        ShipCommandBoards.Reveal(world, SpecialDelivery);

        ShipCommandBoards.Choose(world);

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void SpecialDeliveryInAlterEgoFormSchemesWithPlusOne()
    {
        // "When Revealed (Alter-Ego): ... If you do not, the villain schemes with +1 SCH."
        // Rhino (I) has 1 SCH, so the main scheme gains 2.
        var world = ShipCommandBoards.Board(face: "01001b");
        ShipCommandBoards.PutMilano(world);
        var main = GrootBoards.MainScheme(world);
        ShipCommandBoards.Reveal(world, SpecialDelivery);

        ShipCommandBoards.Choose(world, "schemes");

        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat"));
    }
}
