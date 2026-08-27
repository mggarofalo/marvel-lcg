using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreDroneAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:each-player.1")]
    [Fact]
    public void CrimsonCowlCreatesOneDroneForEachPlayer()
    {
        var world = Board(players: 2);
        SeedDecks(world, cards: 2);
        var scheme = world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, scheme, player: 0);

        Assert.Single(FacedownDrones.EngagedWith(world, 0));
        Assert.Single(FacedownDrones.EngagedWith(world, 1));
    }

    [Rule("rr:lasting-effects")]
    [Fact]
    public void UltronTwoCountsTheDroneItsInterruptCreates()
    {
        var world = Board(players: 1);
        SeedDecks(world, cards: 3);
        FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", []);
        var ultron = world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards, ultron.ObjectId,
            world.Seats[0].IdentityCard.ObjectId, player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == ultron.ObjectId);

        runner.Resolve(world, occurrence, ability, [], []);

        Assert.Collection(
            FacedownDrones.EngagedWith(world, 0),
            _ => { },
            _ => { });
        Assert.Equal(4, StateFields.Modified(world, ultron, "attack", Cards, world.Players));
    }

    [Rule("rr:in-player-order")]
    [Fact]
    public void InvasiveAiDiscardsThreeFromEveryPlayerDeck()
    {
        var world = Board(players: 2);
        SeedDecks(world, cards: 4);
        var invasive = world.CreateCard("01149", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, invasive, player: 0);

        for (int player = 0; player < 2; player++)
        {
            Assert.Single(world.Seats[player].Deck.Cards);
            Assert.Collection(
                world.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards,
                _ => { },
                _ => { },
                _ => { });
        }
    }

    [Rule("rr:first-player")]
    [Fact]
    public void UltronsImperativeCreatesTwoDronesForTheFirstPlayer()
    {
        var world = Board(players: 2);
        world.FirstPlayer = 1;
        SeedDecks(world, cards: 3);
        var imperative = world.CreateCard("01150", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, imperative, player: 0);

        Assert.Empty(FacedownDrones.EngagedWith(world, 0));
        Assert.Collection(
            FacedownDrones.EngagedWith(world, 1),
            _ => { },
            _ => { });
    }

    private static World Board(int players)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }
        return world;
    }

    private static void SeedDecks(World world, int cards)
    {
        foreach (var seat in world.Seats)
        {
            for (int card = 0; card < cards; card++)
            {
                world.CreateCard("01087", seat.Deck);
            }
        }
    }
}
