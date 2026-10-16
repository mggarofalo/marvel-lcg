using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CompiledProgramInterleavingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:activation.7")]
    public void SharedProgramKeepsChoicesQueriesRandomnessAndDelayedEffectsLocal(bool reverseCompletion)
    {
        // Component fixture using printed Core effects, not a full-game transcript.
        // Ancestral Knowledge: choose up to three different discard cards and shuffle
        // them into your deck. "The same target cannot be chosen multiple times."
        // Sonic Boom's boost exhausts your hero if this activation deals damage to you;
        // an activation-initiating effect resolves after the activation fully resolves.
        // Independent interpreter ownership and query purity are engine choices.
        string[][] isolated = [RunIsolated(0), RunIsolated(1)];
        AbilityProgram program = Program();
        var shared = new AbilityRunner(program);
        Board[] boards = [new(shared, 0), new(shared, 1)];
        foreach (int boardIndex in new[] { 0, 1 })
        {
            boards[boardIndex].SuspendChoice();
            Assert.Equal(isolated[boardIndex][0], boards[boardIndex].Snapshot());
        }
        string firstPending = boards[0].Snapshot();
        boards[1].Probe();
        boards[0].Probe();
        Assert.Equal(firstPending, boards[0].Snapshot());
        Assert.Equal(isolated[1][0], boards[1].Snapshot());

        int[] order = reverseCompletion ? [1, 0] : [0, 1];
        foreach (int boardIndex in order)
        {
            string untouched = boards[1 - boardIndex].Snapshot();
            boards[boardIndex].AnswerChoice(boardIndex);
            Assert.Equal(isolated[boardIndex][1], boards[boardIndex].Snapshot());
            Assert.Equal(untouched, boards[1 - boardIndex].Snapshot());
        }
        Assert.NotEqual(boards[0].World.Digest().Canonical(), boards[1].World.Digest().Canonical());
        foreach (int boardIndex in new[] { 0, 1 })
        {
            boards[boardIndex].RegisterBoost();
            Assert.Equal(isolated[boardIndex][2], boards[boardIndex].Snapshot());
        }
        foreach (int boardIndex in order)
        {
            string untouched = boards[1 - boardIndex].Snapshot();
            boards[boardIndex].CompleteActivation();
            Assert.Equal(isolated[boardIndex][3], boards[boardIndex].Snapshot());
            Assert.Equal(untouched, boards[1 - boardIndex].Snapshot());
            Assert.False(boards[boardIndex].World.Seats[0].IdentityCard.Ready);
        }
    }

    private static AbilityProgram Program()
    {
        // A synthetic optional interrupt provides an observable pause after Sonic Boom registers its delayed effect.
        var interrupt = AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","target":"you"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        return AbilityLowering.Book(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities, .. interrupt.Abilities],
        });
    }

    private static string[] RunIsolated(int answer)
    {
        var board = new Board(new AbilityRunner(Program()), answer);
        board.SuspendChoice();
        board.Probe();
        string suspended = board.Snapshot();
        board.AnswerChoice(answer);
        string answered = board.Snapshot();
        board.RegisterBoost();
        string delayed = board.Snapshot();
        board.CompleteActivation();
        return [suspended, answered, delayed, board.Snapshot()];
    }

    private sealed class Board
    {
        private static readonly string[] DiscardFaces = ["01046", "01047", "01048", "01049"];
        private readonly AbilityRunner runner;
        private readonly Card source;
        private readonly Card payment;
        private readonly int[] candidates;
        private readonly List<GameEvent> events = [];
        private Prompt? prompt;
        private Card? interrupt;
        internal World World { get; }

        internal Board(AbilityRunner runner, int variant)
        {
            this.runner = runner;
            World = new World(CardCatalogData, 1, seed: (uint)(91 + variant));
            var seat = World.CreateSeat("player");
            seat.IdentityCard = World.CreateCard("01040a,01040b", seat.Hero);
            seat.IdentityCard.TurnTo("01040b");
            World.CreateCard("01113", World.AreaOf(DeckType.VillainArea));
            source = World.CreateCard("01042", seat.Hand);
            payment = World.CreateCard("01044", seat.Hand);
            World.CreateCard("01045", seat.Deck);
            var discard = World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            candidates = [.. DiscardFaces
                .Select(face => World.CreateCard(face, discard).ObjectId)];
            World.Abilities = runner;
        }

        internal void SuspendChoice()
        {
            events.AddRange(runner.ExecuteAction(World, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [payment.ObjectId], [])).Events);
            prompt = Sequence.Work(World, CardCatalogData, runner, events);
            Assert.NotNull(prompt);
            Assert.Contains(prompt.Affordances, offer => offer.Targets is not null);
        }

        internal void Probe()
        {
            string before = Snapshot();
            var step = Assert.Single(World.Agenda.Outstanding);
            string expected = JsonSerializer.Serialize(prompt);
            for (int query = 0; query < 3; query++)
            {
                Assert.Equal(expected, JsonSerializer.Serialize(runner.Choosing(World, step)));
                _ = runner.Actions(World, 0);
                _ = runner.Constant(World, World.Seats[0].IdentityCard);
            }
            Assert.Equal(before, Snapshot());
        }

        internal void AnswerChoice(int answer)
        {
            var offer = Assert.Single(prompt!.Affordances);
            int[] selected = answer == 0 ? candidates[..3] : [candidates[3], candidates[2], candidates[1]];
            Assert.True(offer.Targets!.Allows(selected));
            Sequence.Answer(World, CardCatalogData, runner, prompt, new Decision(offer.Id, selected), events);
            Sequence.Finish(World, CardCatalogData, runner, events);
            prompt = null;
            Assert.False(World.Agenda.IsBusy);
            Assert.True(World.Random.Generator.WordsConsumed > 0);
            Assert.All(selected, id => Assert.Contains(World.Cards[id], World.Seats[0].Deck.Cards));
        }

        internal void RegisterBoost()
        {
            World.Seats[0].IdentityCard.TurnTo("01040a");
            interrupt = World.CreateCard("01092",
                World.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            var deck = World.AreaOf(DeckType.EncounterDeck);
            World.CreateCard("01101", deck);
            World.CreateCard("01101", deck);
            World.CreateCard("01123", deck);
            World.Agenda.Now(new PhaseStep(new EnemyAttackOperation(), 1, 0,
                Subject: World.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
            for (int questions = 0; questions < 8; questions++)
            {
                prompt = Sequence.Work(World, CardCatalogData, runner, events);
                Assert.NotNull(prompt);
                if (prompt.Affordances.Any(offer => offer.AnchorId == interrupt.ObjectId)) break;
                Assert.True(prompt.Cancellable);
                Sequence.Answer(World, CardCatalogData, runner, prompt, Decision.Decline, events);
            }
            Assert.Contains(prompt!.Affordances, offer => offer.AnchorId == interrupt.ObjectId);
            Assert.True(World.Seats[0].IdentityCard.Ready);
        }

        internal void CompleteActivation()
        {
            Sequence.Answer(World, CardCatalogData, runner, prompt!, Decision.Decline, events);
            Sequence.Finish(World, CardCatalogData, runner, events);
            prompt = null;
            Assert.True(World.Seats[0].IdentityCard.Damage > 0);
            Assert.False(World.IsOver);
        }

        internal string Snapshot() => JsonSerializer.Serialize(new
        {
            Prompt = prompt,
            ConcealedCandidatesExposed = prompt?.ExposesConcealedCandidates,
            Events = events,
            Offers = runner.Actions(World, 0),
            World.Random.Generator.WordsConsumed,
            Digest = World.Digest().Canonical(),
        });
    }
}
