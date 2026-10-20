using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class UncertainLoyaltiesTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    public void PrintedChoicesNeverOfferAFreeFormChange(bool hero, int option)
    {
        var world = Hero("04031a,04031b");
        if (!hero) Forms.Change(world.Seats[0], Cards);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var obligation = world.CreateCard("04053", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.ExecuteReveal(world, obligation, 0);
        var prompt = Sequence.Work(world, Cards, runner, [])!;
        Assert.NotNull(prompt);
        Assert.Equal(hero ? 1 : 2, prompt.Affordances.Count);
        Assert.DoesNotContain(prompt.Affordances, offered => offered.Description?.Contains("form", StringComparison.OrdinalIgnoreCase) == true);
        var chosen = Assert.Single(prompt.Affordances, offered => offered.Id == option);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(chosen.Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(option == 0 ? DeckType.RemovedArea : DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(option == 0 ? 0 : 3, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(option != 0, world.Seats[0].IdentityCard.Ready);
        Assert.Equal(hero, Forms.In(world, world.Seats[0], Cards, Forms.Hero));
    }
}

