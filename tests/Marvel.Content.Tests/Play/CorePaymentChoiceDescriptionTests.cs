using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CorePaymentChoiceDescriptionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("01144a", "Y")]
    [InlineData("01144b", "B")]
    [InlineData("01144c", "R")]
    public void AndroidBoostNamesBothConsequencesAndItsVisibleSourceWithoutRevealingTheDeck(
        string face, string resource)
    {
        // Presentation wording is our choice; the typed cost and Drone profile
        // are the admitted Core card's compiled instructions.
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("Black Panther");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        world.CreateCard("01044", seat.Hand);
        world.CreateCard("01083", seat.Deck);
        Card boost = world.CreateCard(face, world.AreaOf(DeckType.BoostingArea));
        var runner = AuthoredCards.Runner();
        runner.Boost(world, boost, 0);
        var step = Assert.Single(world.Agenda.Outstanding);
        var before = world.Digest().Fingerprint();

        Prompt prompt = runner.Choosing(world, boost, 0, step.Index, step.Tier)!;

        Assert.False(prompt.Cancellable);
        Assert.Equal([boost.ObjectId], prompt.ContextCardIds);
        Assert.Equal("Android Efficiency: choose an option", prompt.DisplayQuestion);
        Affordance spend = Assert.Single(prompt.Affordances, offer => offer.Label == "spend");
        Assert.Equal("Spend resources", spend.DisplayLabel);
        Assert.Equal([resource], Assert.Single(spend.CostOptions).Rule);
        Affordance drone = Assert.Single(prompt.Affordances, offer => offer.Label == "effect");
        Assert.Equal("Engage 1 Drone", drone.DisplayLabel);
        Assert.Equal(drone.DisplayLabel, drone.CommitLabel);
        Assert.Contains("facedown", drone.Description);
        Assert.DoesNotContain("Haymaker", drone.Description);
        Assert.Equal(before, world.Digest().Fingerprint());

        Card villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Attack = new EnemyAttack(villain.ObjectId, 0, seat.IdentityCard.ObjectId);
        Prompt nested = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Contains("Ability choice", nested.Description);
        Assert.DoesNotContain("ChooseOption", nested.Description);
    }
}
