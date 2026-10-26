using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorResourceCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06022", "YY")]
    [InlineData("06023", "BB")]
    [InlineData("06024", "RR")]
    public void ActualDoubleResourcesPayPrintedTwo(string face, string resources)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var helmet = world.CreateCard("06010", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        Assert.Equal(resources, Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, helmet), g => g.Effect == payment.ObjectId).Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], helmet, [payment.ObjectId], []);
        Assert.Equal(DeckType.UpgradesArea, helmet.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Empty(AuthoredCards.Book.On(face));
    }

    [Fact]
    public void GodOfThunderPaysOneEnergyOnlyInHeroFormAndOnlyOncePerExhaustion()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("06008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var hammer = world.CreateCard("06009", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 2; i++)
            Assert.Equal("Y", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, hammer), g => g.Effect == source.ObjectId).Generates);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), g => g.Effect == source.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("06001b");
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("06001a");
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], hammer, [source.ObjectId], []);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
        var state = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteResource(world, 0, source.ObjectId));
        Assert.Equal(state, world.Digest().Canonical());
        source.Refresh();
        Assert.Contains(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
    }

    [Theory]
    [InlineData("01056", "GG")]
    [InlineData("01083", "G")]
    public void AggressionPowerQualifiesTheCardBeingPaid(string target, string expected)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(target, world.Seats[0].Hand);
        var power = world.CreateCard("06016", world.Seats[0].Hand);
        Assert.Equal(expected, Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card), g => g.Effect == power.ObjectId).Generates);
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner), g => g.Effect == power.ObjectId).Generates);
        var extra = world.CreateCard("01003", world.Seats[0].Hand);
        if (expected == "G")
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
        }
        else
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []);
            Assert.Equal(DeckType.SupportsArea, card.Area.Type);
            Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        }
    }
}
