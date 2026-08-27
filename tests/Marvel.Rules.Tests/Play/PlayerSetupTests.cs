using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Setup step 16, between mulligans and the first player turn.</summary>
public sealed class PlayerSetupTests
{
    [Rule("rr:appendix-ii-setup.step.15")]
    [Rule("rr:appendix-ii-setup.step.16")]
    [Fact]
    public void PlayerSetupRunsAfterTheMulliganAndBeforeRoundOne()
    {
        // Step 15 resolves mulligans. Step 16 then says, "Resolve any 'Setup'
        // abilities on player cards in play." The first turn is later still.
        var (world, facts) = Board(1);
        var identity = world.Seats[0].IdentityCard;
        var abilities = new SetupRecorder((0, [identity]));
        var game = Game.Begin(world, facts, abilities);

        Assert.Empty(abilities.Resolved);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(0, game.Round);

        var result = game.Resolve(Decision.Decline);

        Assert.Equal([identity.ObjectId], abilities.Resolved);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, game.Round);
        Assert.Equal(Question.TurnOption, result.Prompt!.Asking);
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Fact]
    public void EveryPlayerResolvesTheirMulliganBeforePlayerSetupBegins()
    {
        // "Each player may discard" at step 15: resolving one player's choice
        // cannot advance to step 16 while another player's choice is open.
        var (world, facts) = Board(2);
        var abilities = new SetupRecorder(
            (0, [world.Seats[0].IdentityCard]),
            (1, [world.Seats[1].IdentityCard]));
        var game = Game.Begin(world, facts, abilities);

        var afterFirst = game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(1, afterFirst.Prompt!.Player);
        Assert.Empty(abilities.Resolved);

        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(2, abilities.Resolved.Count);
    }

    [Rule("rr:in-player-order")]
    [Rule("rr:appendix-ii-setup.step.16")]
    [Fact]
    public void PlayerSetupUsesPlayerOrderAndStableCardOrder()
    {
        // The first player goes first and the others follow clockwise. Object
        // ids give the engine a stable order when one player has several Setup
        // cards and printed text supplies no ordering choice.
        var (world, facts) = Board(2);
        world.FirstPlayer = 1;
        var p0High = world.CreateCard(
            "setup", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var p1Low = world.CreateCard(
            "setup", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var p1High = world.CreateCard(
            "setup", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var abilities = new SetupRecorder(
            (0, [p0High]),
            (1, [p1High, p1Low]));
        var game = Game.Begin(world, facts, abilities);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        Assert.Equal([1, 0], abilities.DiscoveredFor);
        Assert.Equal(
            [p1Low.ObjectId, p1High.ObjectId, p0High.ObjectId],
            abilities.Resolved);
        Assert.Equal(1, game.Active);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:setup-triggered-ability.1")]
    [Rule("rr:appendix-ii-setup.step.16")]
    [Fact]
    public void ASetupChoiceSuspendsAndResumesBeforeTheFirstTurn()
    {
        // Setup abilities are mandatory, but resolving one can still require a
        // choice. The game must retain step 16 until that choice and the rest
        // of the Setup abilities have resolved.
        var (world, facts) = Board(1);
        var first = world.Seats[0].IdentityCard;
        var second = world.CreateCard(
            "setup", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new ChoosingSetup(first.ObjectId, (0, [first, second]));
        var game = Game.Begin(world, facts, abilities);

        var stopped = game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.PlayerSetup, game.Phase);
        Assert.Equal(0, game.Round);
        Assert.Equal("choose setup card", stopped.Prompt!.Label);
        Assert.Equal([first.ObjectId], abilities.Resolved);

        var resumed = game.Resolve(Decision.Take(77));

        Assert.True(abilities.AnsweredChoice);
        Assert.Equal([first.ObjectId, second.ObjectId], abilities.Resolved);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, game.Round);
        Assert.Equal(Question.TurnOption, resumed.Prompt!.Asking);
    }

    private static (World World, Printed Facts) Board(int players)
    {
        var facts = new Printed();
        var world = new World(facts, players);
        for (int player = 0; player < players; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard =
                world.CreateCard("identity", world.Seats[player].Hero);
        }

        return (world, facts);
    }

    private class SetupRecorder(params (int Player, Card[] Cards)[] setup) : NoCardAbilities
    {
        private readonly IReadOnlyDictionary<int, Card[]> cards =
            setup.ToDictionary(item => item.Player, item => item.Cards);

        public List<int> DiscoveredFor { get; } = [];

        public List<int> Resolved { get; } = [];

        public override IReadOnlyList<Card> PlayerSetupCards(World world, int player)
        {
            DiscoveredFor.Add(player);
            return cards.GetValueOrDefault(player) ?? [];
        }

        public override IReadOnlyList<GameEvent> Setup(World world, Card card)
        {
            Resolved.Add(card.ObjectId);
            return [];
        }
    }

    private sealed class ChoosingSetup(
        int choosing,
        params (int Player, Card[] Cards)[] setup) : SetupRecorder(setup)
    {
        public bool AnsweredChoice { get; private set; }

        public override IReadOnlyList<GameEvent> Setup(World world, Card card)
        {
            var events = base.Setup(world, card);
            if (card.ObjectId == choosing)
            {
                world.Agenda.Then(new PhaseStep(
                    Steps.ChooseOption, 0, 16, Subject: card.ObjectId, Seat: 0));
            }

            return events;
        }

        public override Prompt? Choosing(
            World world, Card source, int player, int stoppedAt,
            AbilityType? tier, bool finalStep) =>
            new(
                player,
                Question.Element,
                TimingPriority.Untimed,
                "Setup",
                "choose setup card",
                Cancellable: false,
                [new Affordance(77, "Choose", source.ObjectId, player, "choose")]);

        public override IReadOnlyList<GameEvent> Chose(
            World world, Card source, int player, int stoppedAt, Decision input,
            AbilityType? tier, bool finalStep)
        {
            AnsweredChoice = true;
            return [];
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "setup" => CardKind.Upgrade,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
