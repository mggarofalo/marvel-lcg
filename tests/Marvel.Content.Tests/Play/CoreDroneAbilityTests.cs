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
        world.Abilities = runner;

        runner.WhenRevealed(world, invasive, player: 0);
        var ordering = Sequence.Work(world, Cards, runner, [])!;
        var order = Assert.Single(ordering.Affordances);
        int[] identities = world.Seats
            .Select(seat => seat.IdentityCard.ObjectId)
            .ToArray();
        Sequence.Answer(
            world,
            Cards,
            runner,
            ordering,
            new Decision(order.Id, identities),
            []);
        Sequence.Finish(world, Cards, runner, []);

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

    [Rule("rr:in-play-and-out-of-play.5")]
    [Rule("rr:in-play-and-out-of-play.13")]
    [Fact]
    public void AFacedownHulkDroneDoesNotTriggerHulksPrintedResponse()
    {
        // Only a faceup card's text is active. The object still carries Hulk's
        // face id underneath for the digest, but its completed Drone attack
        // cannot run "After Hulk attacks" from that hidden player card.
        var world = Board(players: 1);
        var hulk = world.CreateCard("01050", world.Seats[0].Deck);
        FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", []);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1,
            [Steps.AttackEnds],
            world,
            Cards,
            hulk.ObjectId,
            world.Seats[0].IdentityCard.ObjectId,
            0);

        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
    }

    [Rule("rr:in-play-and-out-of-play.5")]
    [Rule("rr:in-play-and-out-of-play.13")]
    [Fact]
    public void AFacedownHelicarrierDroneDoesNotOfferHelicarriersAction()
    {
        // The Drone is in an in-play area and retains the underlying player
        // owner, both of which make it easy for an action scan to find. Its
        // facedown text is nevertheless inactive.
        var world = Board(players: 1);
        var helicarrier = world.CreateCard("01092", world.Seats[0].Deck);
        FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", []);
        var runner = AuthoredCards.Runner();

        Assert.DoesNotContain(
            runner.Actions(world, 0),
            ability => ability.Card == helicarrier.ObjectId);
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
