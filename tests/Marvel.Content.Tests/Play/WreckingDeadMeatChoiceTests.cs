using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingDeadMeatChoiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:first-player.1")]
    public void FirstPlayerChoosesTiedAlliesAndAlterEgoIsNotEligible(int selected)
    {
        // 07016 targets "the hero or ally with the fewest remaining hit points."
        // rr:first-player.1: "the first player selects among the eligible options."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0, "01001b", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        world.Seats[0].IdentityCard.TakeDamage(9);
        var allies = world.Seats.Select((seat, player) =>
            world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea,
                seat.Hero.PlayArea, cardOwner: player))).ToArray();
        foreach (var ally in allies) ally.TakeDamage(1);
        runner.ExecuteReveal(world, source, 0);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Assert.Equal(allies.Select(card => card.ObjectId).Order(),
            prompt.Affordances.Select(offer => offer.Id).Order());
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(allies[selected].ObjectId), []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(2, allies[selected].Damage);
        Assert.Equal(1, allies[1 - selected].Damage);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }
}
