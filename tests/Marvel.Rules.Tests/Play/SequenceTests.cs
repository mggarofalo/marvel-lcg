using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// Stopping in the middle of a phase, and picking it up again.
/// </summary>
/// <remarks>
/// The recorded milestone game cannot reach any of this: it has one player, and
/// no card it plays waits in a window. So these boards are built by hand and
/// every claim rests on its citation.
/// </remarks>
public sealed class SequenceTests
{
    [Rule("rr:villain-phase")]
    [Fact]
    public void TheVillainPhaseIsSixStepsAndTheyAreVisible()
    {
        // The Rules Reference lists six, and all six are on the agenda by
        // name rather than being the order of six method calls -- an order a
        // reader would otherwise have to reconstruct.
        //
        // **Step 4 needs a heading of its own.** A deal step that scheduled a
        // reveal per card it dealt would leave a card dealt at any other moment
        // -- by an ability, or by a player's deck running out -- with nothing
        // to reveal it. The rule's own wording is a loop, "until no dealt
        // encounter cards remain", and a loop needs
        // a step to be.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);

        Assert.Equal(
            [Steps.PlaceThreat, Steps.EnemiesActivate, Steps.DealEncounterCards,
             Steps.RevealEncounterCards, Steps.PassFirstPlayerToken, Steps.EndVillainPhase],
            world.Agenda.Outstanding.Select(step => step.What));
    }

    [Rule("rr:ability.step.2")]
    [Rule("rr:ability.step.4")]
    [Fact]
    public void AnOrdinaryPhaseRunsToTheEndWithoutAskingAnything()
    {
        // Every step opens two windows and every one of them is empty, so the
        // whole phase happens inside one answer. That is what makes wrapping
        // every occurrence affordable.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);

        Assert.Null(Sequence.Work(world, new Facts(), new Silent(), []));
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:ability.step.2.c")]
    [Fact]
    public void APhaseStopsWhereAWindowHasSomethingToAsk()
    {
        // The point of all of it: the game can stop in the middle of the
        // villain phase, and what it stopped on is a value on the board rather
        // than a suspended call.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);
        var cards = new Interrupter(Steps.ConditionsOf(Steps.PlaceThreat)[0]);

        var asked = Sequence.Work(world, new Facts(), cards, []);

        Assert.NotNull(asked);
        Assert.Equal(Question.Opportunity, asked.Asking);
        Assert.Equal(Steps.PlaceThreat, world.Agenda.Current!.Value.What);
        Assert.Equal(Stage.Interrupts, world.Agenda.Stage);
        Assert.True(world.Windows.IsResolving);
    }

    [Rule("rr:interrupt.3")]
    [Fact]
    public void DecliningTheInterruptLetsTheStepHappenAndThePhaseFinish()
    {
        // "An interrupt ability is resolved when its triggering condition
        // initiates, but before that triggering condition resolves." Declining
        // is what lets the condition resolve.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);
        var cards = new Interrupter(Steps.ConditionsOf(Steps.PlaceThreat)[0]);
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, new Facts(), cards, events);
        Assert.NotNull(asked);

        Sequence.Answer(world, new Facts(), cards, asked, Decision.Decline, events);

        Assert.Null(Sequence.Work(world, new Facts(), cards, events));
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(cards.Resolved);
    }

    [Rule("rr:interrupt.2")]
    [Fact]
    public void TakingTheInterruptResolvesItAndTheStepStillHappens()
    {
        // "Each interrupt can only be triggered once per occurrence of the
        // triggering condition", so having used it the player is not asked
        // again and the phase carries on.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);
        var cards = new Interrupter(Steps.ConditionsOf(Steps.PlaceThreat)[0]);
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, new Facts(), cards, events);
        Sequence.Answer(world, new Facts(), cards, asked!, Decision.Take(Interrupter.Handle), events);

        Assert.Equal([Interrupter.Card], cards.Resolved);
        Assert.Null(Sequence.Work(world, new Facts(), cards, events));
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:forced.1")]
    [Fact]
    public void AnOrderingQuestionCannotBeDeclined()
    {
        // A forced ability must resolve, so "none of them" is not an answer to
        // "in what order?".
        var world = Board(players: 1);
        var occurrence = new Occurrence(1, "WhenAttacked");
        var cards = new Interrupter("WhenAttacked");
        world.Windows.Open(occurrence, WindowKind.Interrupt);

        var asked = new Prompt(0, Question.Order, TimingPriority.ForcedInterrupt,
                               "WhenAttacked", "in what order?", false, []);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Sequence.Answer(world, new Facts(), cards, asked, Decision.Decline, []));
        Assert.Contains("cannot be declined", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAnswerNamingSomethingThatWasNotOfferedIsRefused()
    {
        var world = Board(players: 1);
        var cards = new Interrupter("WhenAttacked");
        world.Windows.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);
        var asked = new Prompt(0, Question.Opportunity, TimingPriority.Interrupt,
                               "WhenAttacked", "interrupt?", true, []);

        Assert.Throws<RulesNotImplementedException>(
            () => Sequence.Answer(world, new Facts(), cards, asked, Decision.Take(999), []));
    }

    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    [Fact]
    public void TheVillainWinningAbandonsTheRestOfThePhase()
    {
        // "If the villain completes the final stage of the main scheme deck,
        // the villain wins the game." The encounter cards are not dealt, the
        // token is not passed, and the round does not end.
        var world = Board(players: 1);
        VillainPhase.Schedule(world.Agenda, round: 1);
        var cards = new Silent();

        // Step 1 places threat; this board's scheme completes on it.
        Sequence.Work(world, new Facts(overThreshold: true), cards, []);

        // And *which* ending it was, which a boolean could not say. The other
        // one is `rr:villain-defeat`'s, and a game that reported only "over"
        // could not tell a player whether they had won.
        Assert.Equal(Outcome.VillainWins, world.Result);
        Assert.True(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
    }

    private static World Board(int players)
    {
        var world = new World(new Facts(), players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("identity", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));

        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateCard("encounter", deck);
            world.CreateCard("boost", deck);
        }

        return world;
    }

    /// <summary>No card waits in any window.</summary>
    private class Silent : NoCardAbilities
    {

        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
        {
            Resolved.Add(ability.Card);
            return [];
        }

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(Handle, "Use", ability.Card, ability.Player, "an interrupt");

        public List<int> Resolved { get; } = [];

        /// <summary>The affordance id this offers its one ability under.</summary>
        public const int Handle = 77;

        /// <summary>The object id of the card carrying it.</summary>
        public const int Card = 3;
    }

    /// <summary>One optional interrupt, on one named condition.</summary>
    private sealed class Interrupter(string condition) : Silent
    {
        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Interrupt
            && occurrence.Conditions.Contains(condition)
            && !Resolved.Contains(Card)
                ? [new PendingAbility(Card, AbilityType.Interrupt, 0)]
                : [];
    }

    private sealed class Facts(bool overThreshold = false) : ICardFacts
    {
        private readonly Dictionary<string, CardKind> kinds = new(StringComparer.Ordinal)
        {
            ["identity"] = CardKind.AlterEgo,
            ["villain"] = CardKind.EncounterVillain,
            ["scheme"] = CardKind.MainScheme,
            ["boost"] = CardKind.Treachery,
            ["encounter"] = CardKind.Treachery,
        };

        public CardKind Kind(string faceId) =>
            kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId == "scheme"
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["EscalationThreat"] = overThreshold ? "5" : "0",
                    ["TargetThreat"] = "4",
                }
                : new Dictionary<string, string>(StringComparer.Ordinal) { ["SCH"] = "0" };

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out var printed)
            && long.TryParse(printed, out long value) ? value : fallback;
    }
}
