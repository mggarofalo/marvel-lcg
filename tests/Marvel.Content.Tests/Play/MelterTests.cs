using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MelterTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void AReadyControlledAllyIsTheRequiredDefender()
    {
        // "The engaged player must defend ... with an ally they control, if
        // able." A hero and another player's ally remain legal by the general
        // defense rules, but Melter removes them from this choice.
        var (world, melter, hero, controlled, other) = Board();
        var choice = AuthoredCards.Runner().Defenders(
            world,
            new EnemyAttack(melter.ObjectId, Player: 0, Target: hero.ObjectId),
            [hero, controlled, other]);

        Assert.True(choice.Required);
        Assert.Same(controlled, Assert.Single(choice.Candidates));
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void WithNoReadyControlledAllyOrdinaryDefenseRemainsOptional()
    {
        // "If able" does not make an impossible cost mandatory. Once the
        // engaged player's only ally is exhausted, the hero and the other
        // player's ally remain the ordinary optional defenders.
        var (world, melter, hero, controlled, other) = Board();
        controlled.Exhaust();
        Card[] candidates = [hero, other];

        var choice = AuthoredCards.Runner().Defenders(
            world,
            new EnemyAttack(melter.ObjectId, Player: 0, Target: hero.ObjectId),
            candidates);

        Assert.False(choice.Required);
        Assert.Equal(candidates, choice.Candidates);
    }

    private static (World World, Card Melter, Card Hero, Card Controlled, Card Other) Board()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }

        var melter = world.CreateCard(
            "01132", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var controlled = world.CreateCard(
            "01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard(
            "01051", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        return (world, melter, world.Seats[0].IdentityCard, controlled, other);
    }
}
