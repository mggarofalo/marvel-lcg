using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Descriptions are engine presentation choices; the authored Core DSL owns costs.
public sealed class CoreAbilityCostDescriptionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("01064", "Exhaust Surveillance Team", "Remove 1 snoop counter from Surveillance Team")]
    [InlineData("01056", "Exhaust Tac Team", "Remove 1 attack counter from Tac Team")]
    public void SupportOfferNamesMandatoryExhaustionAndCounterCostBeforeEitherIsPaid(
        string face, string exhaustion, string counter)
    {
        var world = new World(Cards, players: 1);
        Seat seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        Card source = world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea)).PlaceTokens("k_threat", 3);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.EnterPlay(world, Cards, source, [], abilities: runner);
        string before = world.Digest().Canonical();

        var ability = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        string? description = runner.Describe(world, ability).CostDescription;

        Assert.Contains(exhaustion, description);
        Assert.Contains(counter, description);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(source.Ready);
    }
}
