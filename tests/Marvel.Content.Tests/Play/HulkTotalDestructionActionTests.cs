using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkTotalDestructionActionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryBasicThwartRemovesThreatOnlyWithoutAbomination(bool present)
    {
        var (world, runner) = HulkNemesisFixture.Create();
        Forms.Change(world.Seats[0], world.Facts);
        var scheme = HulkNemesisFixture.Dealt(world, "10027");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        if (present) world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.Equal(!present, BasicPowers.Thwartable(world, world.Facts, 0).Contains(scheme));
        if (!present)
        {
            BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
            WreckingRemainingSchemeFixture.Finish(world, runner);
        }
        Assert.Equal(present ? 4 : 3, scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrintedThwartEventRemovesThreatOnlyWithoutAbomination(bool present)
    {
        var (world, runner) = HulkNemesisFixture.Create();
        Forms.Change(world.Seats[0], world.Facts);
        var scheme = HulkNemesisFixture.Dealt(world, "10027");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        if (present) world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = world.CreateCard("10019", world.Seats[0].Hand);
        var payment = world.CreateCard("01090", world.Seats[0].Hand);
        var actions = runner.Actions(world, 0).Where(offer => offer.Card == source.ObjectId).ToArray();
        if (present)
        {
            Assert.Empty(actions);
            Assert.Same(world.Seats[0].Hand, source.Area);
            Assert.Same(world.Seats[0].Hand, payment.Area);
        }
        else
        {
            var action = Assert.Single(actions);
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [scheme.ObjectId]));
            WreckingRemainingSchemeFixture.Finish(world, runner);
            Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        }
        Assert.Equal(present ? 4 : 2, scheme.Tokens["k_threat"]);
    }
}
