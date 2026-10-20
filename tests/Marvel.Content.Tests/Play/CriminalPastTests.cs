using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CriminalPastTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CriminalPastResolvesItsChosenCostAndDeparture(bool remove)
    {
        var world = Hero("04001a,04001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[0].Deck);
        var bow = world.CreateCard("04002", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var obligation = world.CreateCard("04026", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        var events = runner.ExecuteReveal(world, obligation, 0).Events.ToList();
        var form = Sequence.Work(world, Cards, runner, events)!;
        Assert.False(form.Cancellable);
        Sequence.Answer(world, Cards, runner, form, Decision.Take(remove ? 0 : 1), events);
        var consequence = Sequence.Work(world, Cards, runner, events)!;
        Assert.False(consequence.Cancellable);
        Sequence.Answer(world, Cards, runner, consequence, Decision.Take(remove ? 0 : 1), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(remove ? "04001b" : "04001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(!remove, world.Seats[0].IdentityCard.Ready);
        Assert.Equal(remove ? DeckType.RemovedArea : DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(remove ? DeckType.UpgradesArea : DeckType.DiscardPile, bow.Area.Type);
    }
}
