using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Drang, his Spear and the Brotherhood of Badoon schemes over the production book.
public sealed class BadoonSchemeTests : CoreCardTestBase
{
    [Theory]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.EncounterDiscardPile)]
    [Rule("rr:search")]
    public void DrangTwoFindsHisSpearAndItAttachesToHim(DeckType from)
    {
        // 16059: "When Revealed: If Drang's Spear is in play, give Drang 1 facedown boost card;
        // otherwise, search the encounter deck and discard pile for Drang's Spear, reveal it,
        // and shuffle the encounter deck." 16064: "Attach to Drang."
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangTwo);
        var drang = GrootBoards.Villain(world);
        var spear = world.CreateCard(BadoonBoards.Spear, world.AreaOf(from));

        GrootBoards.Runner(world).ExecuteReveal(world, drang, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(drang.ObjectId, spear.Area.Host);
        Assert.DoesNotContain(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.6")]
    public void DrangTwoTakesAFacedownBoostCardWhenTheSpearIsAlreadyInPlay()
    {
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangTwo);
        var drang = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, drang, BadoonBoards.Spear);

        GrootBoards.Runner(world).ExecuteReveal(world, drang, 0);
        BadoonBoards.Drive(world);

        var boost = Assert.Single(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
        Assert.Equal(drang.ObjectId, boost.Area.Host);
    }

    [Fact]
    [Rule("rr:stalwart")]
    public void DrangsSpearGivesHimStalwartAndOneAttack()
    {
        // 16064: "Drang gains stalwart. (He cannot be stunned or confused.)" ATK+1.
        var world = BadoonBoards.Board();
        var drang = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, drang, BadoonBoards.Spear);

        Assert.Equal(1, StateFields.Modified(world, drang, "stalwart", world.Facts, world.Players));
        Assert.Equal(3, StateFields.Modified(world, drang, "attack", world.Facts, world.Players));
    }

    [Fact]
    [Rule("rr:cost")]
    public void DrangsSpearIsDiscardedForMentalAndTwoPhysicalResources()
    {
        // 16064: "Hero Action: Spend [mental][physical][physical] resources → discard this card."
        var world = BadoonBoards.Board();
        var drang = GrootBoards.Villain(world);
        var spear = ShipCommandBoards.Attach(world, drang, BadoonBoards.Spear);
        var genius = GrootBoards.Hand(world, ShipCommandBoards.Genius);
        var first = GrootBoards.Hand(world, ShipCommandBoards.Strength);
        var second = GrootBoards.Hand(world, ShipCommandBoards.Strength);

        ShipCommandBoards.Act(world, spear, 0, genius, first, second);

        Assert.Equal(DeckType.EncounterDiscardPile, spear.Area.Type);
        Assert.Equal(0, StateFields.Modified(world, drang, "stalwart", world.Facts, world.Players));
    }

    [Fact]
    [Rule("rr:cost")]
    public void DrangsSpearIsNotOfferedToAnAlterEgo()
    {
        var world = BadoonBoards.Board(face: "01001b");
        var spear = ShipCommandBoards.Attach(world, GrootBoards.Villain(world), BadoonBoards.Spear);
        GrootBoards.Hand(world, ShipCommandBoards.Genius);
        GrootBoards.Hand(world, ShipCommandBoards.Strength);
        GrootBoards.Hand(world, ShipCommandBoards.Strength);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, spear, 0));
    }

    [Theory]
    [InlineData("16066", 5)]
    [InlineData("16067", 5)]
    [InlineData("16068", 5)]
    [InlineData("16069", 5)]
    [Rule("rr:first-player")]
    public void EachSideSchemesFirstPlayerActionExhaustsTheMilanoToRemoveThreeThreat(string face, int threat)
    {
        // "First Player Action: Exhaust the Milano → remove 3 threat from this scheme."
        var world = BadoonBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var scheme = GrootBoards.Side(world, face, threat);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, scheme, 1));
        ShipCommandBoards.Act(world, scheme, 0);

        Assert.False(milano.Ready);
        Assert.Equal(threat - 3, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void AnExhaustedMilanoOffersNoFirstPlayerAction()
    {
        var world = BadoonBoards.Board();
        ShipCommandBoards.PutMilano(world).Exhaust();

        Assert.Empty(ShipCommandBoards.ActionsOn(world, GrootBoards.MainScheme(world), 0));
    }

    [Fact]
    [Rule("rr:first-player")]
    public void TerrestrialInvasionsFirstPlayerActionRemovesThreeThreat()
    {
        var world = BadoonBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);

        ShipCommandBoards.Act(world, scheme, 0);

        Assert.False(milano.Ready);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void ProtectThePlanetMayDealThreeDamageToAMinion()
    {
        // 16062b: "Exhaust the Milano → choose to either remove 3 threat from this scheme or
        // deal 3 damage to a minion."
        var world = BadoonBoards.Board(scheme: BadoonBoards.ProtectThePlanet);
        ShipCommandBoards.PutMilano(world);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);
        var sentry = GrootBoards.Engaged(world, BadoonBoards.Sentry);
        var action = Assert.Single(ShipCommandBoards.ActionsOn(world, scheme, 0));
        world.Agenda.AddPlayerAction(0, new PlayerAction(action, [], []));

        BadoonBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Description?.StartsWith("Deal 3 damage to a minion", StringComparison.Ordinal) == true) is { } damage
                ? Decision.Take(damage.Id)
                : prompt.Affordances.FirstOrDefault(a => a.Targets?.Legal.Contains(sentry.ObjectId) == true) is { } target
                    ? new Decision(target.Id, [sentry.ObjectId])
                    : prompt.Affordances.FirstOrDefault(a => a.AnchorId == sentry.ObjectId) is { } anchored
                        ? Decision.Take(anchored.Id) : null);

        Assert.Equal(3, sentry.Damage);
        Assert.Equal(5, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void ProtectThePlanetRemovesThreatWhenNoMinionCanBeDamaged()
    {
        // Without a minion the damage option has no valid target.
        var world = BadoonBoards.Board(scheme: BadoonBoards.ProtectThePlanet);
        ShipCommandBoards.PutMilano(world);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);
        var action = Assert.Single(ShipCommandBoards.ActionsOn(world, scheme, 0));
        world.Agenda.AddPlayerAction(0, new PlayerAction(action, [], []));
        var asked = new List<Prompt>();

        BadoonBoards.Drive(world, prompt => { asked.Add(prompt); return null; });

        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.DoesNotContain(asked, prompt => prompt.Affordances.Any(a => a.Description?.StartsWith("Deal 3 damage to a minion", StringComparison.Ordinal) == true));
    }
}
