using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PrintedTextMeaningTests
{
    [Fact]
    public void ActualRobotProjectsBlankingWithItsCostsAndDuration()
    {
        // The checked actual body supplies meaning; no printed-text parsing is involved.
        World world = PersistentFixture.Board();
        Card robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, robot);
        Assert.Equal(robot.ObjectId, facts.Source.CardId);
        CardPersistentAbilityDescriptor action = Assert.Single(facts.Abilities,
            ability => ability.Trigger.Timing == "Action");
        Assert.Equal("this", action.Trigger.Subject);
        Assert.Equal("hero", action.Trigger.Form);
        CardPersistentCostDescriptor cost = Assert.Single(action.Costs);
        Assert.Equal(("SpendResources", "ActingPlayer", "B"), (cost.Operation, cost.Target, cost.Resources));
        CardPersistentEffectDescriptor effect = Assert.Single(action.Effects);
        Assert.Equal(("BlankPrintedText", "Source", TimingPoints.EndOfPhase),
            (effect.Operation, effect.Target, effect.Until));
        Assert.Null(effect.Amount);
        Assert.Null(effect.Field);
    }

    [Fact]
    public void OrdinaryGrantRetainsItsTargetFieldAndNumericAmount()
    {
        World world = PersistentFixture.Board();
        Card villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card blasters = PersistentFixture.Attach(world, "01153", villain);
        CardPersistentAbilityDescriptor constant = Assert.Single(PersistentFixture.Facts(world, blasters).Abilities,
            ability => ability.Trigger.Timing == "Constant");
        CardPersistentEffectDescriptor ordinary = Assert.Single(constant.Effects);
        Assert.Equal(("Grant", "Host", "retaliate", 1L),
            (ordinary.Operation, ordinary.Target, ordinary.Field, ordinary.Amount));
    }
}
