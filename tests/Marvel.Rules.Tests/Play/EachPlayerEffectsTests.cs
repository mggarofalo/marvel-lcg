using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class EachPlayerEffectsTests
{
    [Rule("rr:each-player.1")]
    [Fact]
    public void FirstPlayerChoosesAnExactPermutationAndFramesPersistThatOrder()
    {
        // "If an 'each player' effect does not specify an order in which to
        // resolve the effect, the first player determines the order."
        var world = Board(3);
        world.FirstPlayer = 1;
        var source = world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDiscardPile));
        var abilities = new Recorder();
        EachPlayerEffects.Schedule(
            world, source, stoppedAt: 4, AbilityType.WhenRevealed, finalStep: true);

        var asked = Sequence.Work(world, world.Facts, abilities, []);

        Assert.NotNull(asked);
        Assert.Equal(1, asked.Player);
        Assert.Equal(Question.Order, asked.Asking);
        var order = Assert.Single(asked.Affordances);
        var identities = world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray();
        Assert.Equal(identities.Order(), order.Targets!.Legal.Order());

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(
            world,
            world.Facts,
            abilities,
            asked,
            new Decision(order.Id, [identities[2], identities[2], identities[1]]),
            []));

        Sequence.Answer(
            world,
            world.Facts,
            abilities,
            asked,
            new Decision(order.Id, [identities[2], identities[0], identities[1]]),
            []);
        Sequence.Finish(world, world.Facts, abilities, []);

        Assert.Equal([2, 0, 1], abilities.Players);
        Assert.All(abilities.Positions, position => Assert.Equal(4, position));
        Assert.All(abilities.Tiers, tier => Assert.Equal(AbilityType.WhenRevealed, tier));
        Assert.Equal([false, false, true], abilities.FinalPlayers);
        Assert.All(abilities.FinalSteps, Assert.True);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachSeatRecomputesItsFormWhenItsOwnFrameBegins()
    {
        var world = Board(2);
        var source = world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDiscardPile));
        var abilities = new FormRecorder(changeBefore: 0);
        EachPlayerEffects.Schedule(world, source, stoppedAt: 1);

        var asked = Sequence.Work(world, world.Facts, abilities, []);
        var affordance = Assert.Single(asked!.Affordances);
        var identities = world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray();

        // Seat 1 resolves first and flips seat 0 before seat 0's independent
        // frame begins. Seat 0 must see the current face, not a form captured
        // while the order was chosen.
        Sequence.Answer(
            world,
            world.Facts,
            abilities,
            asked,
            new Decision(affordance.Id, [identities[1], identities[0]]),
            []);
        Sequence.Finish(world, world.Facts, abilities, []);

        Assert.Equal([(1, CardKind.AlterEgo), (0, CardKind.Hero)], abilities.Seen);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void OneAvailablePlayerResolvesWithoutAnOrderingPrompt()
    {
        var world = Board(1);
        var source = world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDiscardPile));
        var abilities = new Recorder();
        EachPlayerEffects.Schedule(world, source, stoppedAt: 2);

        Assert.Null(Sequence.Work(world, world.Facts, abilities, []));
        Assert.Equal([0], abilities.Players);
        Assert.Equal([true], abilities.FinalPlayers);
    }

    [Rule("rr:resource.1")]
    [Fact]
    public void PrintedResourceCountCountsBothIconsOnADoubleResourceCard()
    {
        var world = Board(1);
        var hand = world.Seats[0].Hand;
        var doubleEnergy = world.CreateCard("double-energy", hand);
        var oneEnergy = world.CreateCard("one-energy", hand);
        var mental = world.CreateCard("mental", hand);

        Assert.Equal(
            3,
            Resources.PrintedCount(
                [doubleEnergy, oneEnergy, mental], Resources.Energy, world.Facts));
    }

    private static World Board(int players)
    {
        var world = new World(new Facts(), players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            var identity = world.CreateCard("alter-ego,hero", seat.Hero);
            seat.IdentityCard = identity;
        }

        return world;
    }

    private class Recorder : NoCardAbilities
    {
        public List<int> Players { get; } = [];
        public List<int> Positions { get; } = [];
        public List<AbilityType?> Tiers { get; } = [];
        public List<bool> FinalSteps { get; } = [];
        public List<bool> FinalPlayers { get; } = [];

        public override IReadOnlyList<GameEvent> ResolveEachPlayer(
            World world, Card source, int player, int stoppedAt,
            AbilityType? tier, bool finalStep, bool finalPlayer)
        {
            Players.Add(player);
            Positions.Add(stoppedAt);
            Tiers.Add(tier);
            FinalSteps.Add(finalStep);
            FinalPlayers.Add(finalPlayer);
            return [];
        }
    }

    private sealed class FormRecorder(int changeBefore) : Recorder
    {
        public List<(int Player, CardKind Form)> Seen { get; } = [];

        public override IReadOnlyList<GameEvent> ResolveEachPlayer(
            World world, Card source, int player, int stoppedAt,
            AbilityType? tier, bool finalStep, bool finalPlayer)
        {
            Seen.Add((player, world.Facts.Kind(world.Seats[player].IdentityCard.FaceId)));
            if (player != changeBefore)
            {
                world.Seats[changeBefore].IdentityCard.TurnTo("hero");
            }

            return base.ResolveEachPlayer(
                world, source, player, stoppedAt, tier, finalStep, finalPlayer);
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "alter-ego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "treachery" => CardKind.Treachery,
            _ => CardKind.Event,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId switch
            {
                "double-energy" => new Dictionary<string, string> { ["RES"] = "YY" },
                "one-energy" => new Dictionary<string, string> { ["RES"] = "Y" },
                "mental" => new Dictionary<string, string> { ["RES"] = "B" },
                _ => new Dictionary<string, string>(),
            };

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
