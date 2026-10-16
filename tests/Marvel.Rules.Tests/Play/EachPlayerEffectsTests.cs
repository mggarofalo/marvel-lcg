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
    private sealed record TestFrame(AbilityType? Tier, bool FinalStep) : CardAbilityContinuation;

    private static void Schedule(
        World world, Card source, int stoppedAt, AbilityType? tier = null,
        bool finalStep = false) => EachPlayerEffects.Schedule(world, new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.ResolvePlayer)
            { Continuation = new TestFrame(tier, finalStep) },
            world.Agenda.Current?.Round ?? 0, 2,
            Index: stoppedAt, Subject: source.ObjectId, Plan: true));
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
        Schedule(
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
        Schedule(world, source, stoppedAt: 1);

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
        Schedule(world, source, stoppedAt: 2);

        Assert.Null(Sequence.Work(world, world.Facts, abilities, []));
        Assert.Equal([0], abilities.Players);
        Assert.Equal([true], abilities.FinalPlayers);
    }

    [Fact]
    public void AChoiceSuspendsAndResumesWithItsOwnPlayerFrameContext()
    {
        var world = Board(2);
        var source = world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDiscardPile));
        var abilities = new ChoiceRecorder();
        Schedule(
            world, source, stoppedAt: 7, AbilityType.WhenRevealed, finalStep: true);

        var ordering = Sequence.Work(world, world.Facts, abilities, []);
        var order = Assert.Single(ordering!.Affordances);
        var identities = world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray();
        Sequence.Answer(
            world,
            world.Facts,
            abilities,
            ordering,
            new Decision(order.Id, identities),
            []);

        var first = Sequence.Work(world, world.Facts, abilities, []);
        Assert.NotNull(first);
        Assert.Equal(0, first.Player);
        Assert.Equal(8, world.Agenda.Current!.Value.Index);
        Assert.IsType<TestFrame>(world.Agenda.Current.Value.Require<AbilityContinuationAgendaOperation>().Continuation);
        Assert.False(world.Agenda.Current.Value.Require<AbilityContinuationAgendaOperation>().FinalPlayer);
        Sequence.Answer(
            world, world.Facts, abilities, first, Decision.Take(ChoiceRecorder.Option), []);

        var second = Sequence.Work(world, world.Facts, abilities, []);
        Assert.NotNull(second);
        Assert.Equal(1, second.Player);
        Assert.Equal(8, world.Agenda.Current!.Value.Index);
        Assert.IsType<TestFrame>(world.Agenda.Current.Value.Require<AbilityContinuationAgendaOperation>().Continuation);
        Assert.True(world.Agenda.Current.Value.Require<AbilityContinuationAgendaOperation>().FinalPlayer);
        Sequence.Answer(
            world, world.Facts, abilities, second, Decision.Take(ChoiceRecorder.Option), []);
        Assert.Null(Sequence.Work(world, world.Facts, abilities, []));

        Assert.Equal([(0, false), (1, true)], abilities.Asked);
        Assert.Equal([(0, false), (1, true)], abilities.Answered);
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
            World world, PhaseStep step)
        {
            var operation = step.Require<AbilityContinuationAgendaOperation>();
            var frame = Assert.IsType<TestFrame>(operation.Continuation);
            int player = step.Seat;
            int stoppedAt = step.Index;
            var source = world.Cards[step.Subject];
            var tier = frame.Tier;
            bool finalStep = frame.FinalStep;
            bool finalPlayer = operation.FinalPlayer;
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
            World world, PhaseStep step)
        {
            var operation = step.Require<AbilityContinuationAgendaOperation>();
            var frame = Assert.IsType<TestFrame>(operation.Continuation);
            int player = step.Seat;
            int stoppedAt = step.Index;
            var source = world.Cards[step.Subject];
            var tier = frame.Tier;
            bool finalStep = frame.FinalStep;
            bool finalPlayer = operation.FinalPlayer;
            Seen.Add((player, world.Facts.Kind(world.Seats[player].IdentityCard.FaceId)));
            if (player != changeBefore)
            {
                world.Seats[changeBefore].IdentityCard.TurnTo("hero");
            }

            return base.ResolveEachPlayer(world, step);
        }
    }

    private sealed class ChoiceRecorder : Recorder
    {
        public const int Option = 55;

        public List<(int Player, bool FinalPlayer)> Asked { get; } = [];
        public List<(int Player, bool FinalPlayer)> Answered { get; } = [];

        public override IReadOnlyList<GameEvent> ResolveEachPlayer(
            World world, PhaseStep step)
        {
            var operation = step.Require<AbilityContinuationAgendaOperation>();
            var frame = Assert.IsType<TestFrame>(operation.Continuation);
            int player = step.Seat;
            int stoppedAt = step.Index;
            var source = world.Cards[step.Subject];
            var tier = frame.Tier;
            bool finalStep = frame.FinalStep;
            bool finalPlayer = operation.FinalPlayer;
            world.Agenda.Then(step with
            {
                Operation = operation with { Stage = AbilityContinuationStage.Choose },
                Index = stoppedAt + 1,
            });
            return [];
        }

        public override Prompt? Choosing(
            World world, PhaseStep step)
        {
            var operation = step.Require<AbilityContinuationAgendaOperation>();
            Assert.IsType<TestFrame>(operation.Continuation);
            int player = step.Seat;
            var source = world.Cards[step.Subject];
            bool finalPlayer = operation.FinalPlayer;
            Asked.Add((player, finalPlayer));
            return new Prompt(
                player,
                Question.Option,
                TimingPriority.Untimed,
                Steps.ChooseOption,
                "choose for this player",
                false,
                [new Affordance(Option, "Choose", source.ObjectId, player, "option")]);
        }

        public override IReadOnlyList<GameEvent> Chose(
            World world, PhaseStep step, Decision input)
        {
            var operation = step.Require<AbilityContinuationAgendaOperation>();
            Assert.IsType<TestFrame>(operation.Continuation);
            int player = step.Seat;
            bool finalPlayer = operation.FinalPlayer;
            Assert.Equal(Option, input.Affordance);
            Answered.Add((player, finalPlayer));
            return [];
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
