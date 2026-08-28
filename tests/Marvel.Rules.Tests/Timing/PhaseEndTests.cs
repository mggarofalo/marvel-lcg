using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// Ending a phase: what expires, when, and who gets asked.
/// </summary>
public sealed class PhaseEndTests
{
    [Rule("rr:villain-phase.step.6.a")]
    [Rule("rr:lasting-effects.5")]
    [Fact]
    public void EndingTheVillainPhaseEndsWhatWasBoundToTheRound()
    {
        // "Any effects that last 'until the end of the [villain] phase' or
        // 'until the end of the round' end." Both points, one step.
        var world = Board();
        world.Effects.Register(Lasting(TimingPoints.EndOfVillainPhase));
        world.Effects.Register(Lasting(TimingPoints.EndOfRound));
        world.Effects.Register(Lasting(TimingPoints.EndOfPlayerPhase));

        PhaseEnd.EndVillainPhase(world, new Facts(), []);

        var left = Assert.Single(world.Effects.Active());
        Assert.Equal(TimingPoints.EndOfPlayerPhase, left.Lasts!.Until);
    }

    [Rule("rr:end-of-player-phase.step.4")]
    [Fact]
    public void EndingThePlayerPhaseDoesNotEndWhatWasBoundToTheRound()
    {
        // The player phase ends inside the round, so an effect lasting until
        // the end of the round outlives it. Expiring both here would end a
        // lasting effect half a round early, on a board that looks normal.
        var world = Board();
        world.Effects.Register(Lasting(TimingPoints.EndOfPlayerPhase));
        world.Effects.Register(Lasting(TimingPoints.EndOfRound));

        PhaseEnd.EndPlayerPhase(world, []);

        var left = Assert.Single(world.Effects.Active());
        Assert.Equal(TimingPoints.EndOfRound, left.Lasts!.Until);
    }

    [Rule("rr:temporary.1")]
    [Rule("rr:interrupt.3")]
    [Fact]
    public void TheInterruptWindowOpensBeforeAnythingExpires()
    {
        // `rr:temporary.1` makes the temporary keyword "Forced Interrupt: When
        // the round ends, discard this card from play", and `rr:interrupt.3`
        // puts a forced interrupt before its triggering condition resolves. So
        // a temporary card is discarded *before* step 6a, and an ability that
        // reads the board in that window must see the effects still in force.
        var world = Board();
        world.Effects.Register(Lasting(TimingPoints.EndOfRound));
        var watcher = new Watcher(world);

        EndRound(world, watcher);

        Assert.Equal(1, watcher.ActiveAt(WindowKind.Interrupt));
        Assert.Equal(0, watcher.ActiveAt(WindowKind.Response));
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void TheVillainPhaseEndingIsOneOccurrenceCarryingTwoConditions()
    {
        // The phase ends and the round ends at the same moment, so an ability
        // answering "when the round ends" gets one turn, not two. A window per
        // condition would let it fire twice.
        var world = Board();
        var watcher = new Watcher(world);

        EndRound(world, watcher);

        Assert.Equal(2, watcher.Windows);
        Assert.All(watcher.Seen, occurrence => Assert.Equal(
            [PhaseEnd.VillainPhaseEnds, PhaseEnd.RoundEnds], occurrence.Conditions));
    }

    [Rule("rr:villain-phase.step.6.b")]
    [Fact]
    public void AfterTheRoundEndsEffectsResolveAfterExpiration()
    {
        // Step 6b resolves "when/after the villain phase ends" and
        // "when/after the round ends" effects. A forced response is therefore
        // resolved in the response window after step 6a has expired the round.
        var world = Board();
        world.Effects.Register(Lasting(TimingPoints.EndOfRound));
        var waiting = new Offering(
            new PendingAbility(0, AbilityType.ForcedResponse, 0));

        EndRound(world, waiting);

        Assert.Equal(1, waiting.Resolved);
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void AnOptionalAbilityInAWindowSaysSoRatherThanBeingDeclined()
    {
        // Declining on the player's behalf would produce a board that is right
        // about everything except the ability nobody was offered. Offering it
        // needs a prompt that can name a window -- MARVEL-179.
        var world = Board();
        var waiting = new Offering(new PendingAbility(0, AbilityType.Response, 0));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => EndRound(world, waiting));
        Assert.Contains("nobody to ask", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TwoForcedAbilitiesAtOneMomentSayThatTheFirstPlayerMustChoose()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order." Picking one silently would be
        // choosing on their behalf, and `rr:forced.6` means the choice is
        // observable: each resolves completely before the next initiates.
        var world = Board();
        var waiting = new Offering(
            new PendingAbility(0, AbilityType.ForcedResponse, 0),
            new PendingAbility(1, AbilityType.ForcedResponse, 0));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => EndRound(world, waiting));
        Assert.Contains("in what order", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:delayed-effect.1")]
    [Fact]
    public void ADelayedEffectComingDueSaysSoRatherThanBeingDropped()
    {
        // "Delayed effects resolve automatically and immediately after their
        // specified timing point or future condition occurs." One that came due
        // and did nothing is a rule that silently did not happen -- so a kind
        // nothing knows how to resolve is named rather than dropped.
        var world = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, "deal-damage", Amount: 2,
            Lasts: Duration.NextTime(PhaseEnd.RoundEnds)));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => PhaseEnd.EndVillainPhase(world, new Facts(), []));
        Assert.Contains("came due", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>Schedules step 6 and walks it, windows and all.</summary>
    private static void EndRound(World world, ICardAbilities abilities)
    {
        world.Agenda.Add(new PhaseStep(Steps.EndVillainPhase, Round: 1, Number: 6));
        Sequence.Finish(world, new Facts(), abilities, []);
    }

    private static ContinuousEffect Lasting(string timingPoint) => new(
        EffectSource.LastingEffect, "attack", Amount: 1,
        Lasts: Duration.UntilEndOf(timingPoint));

    private static World Board()
    {
        var world = new World(new Facts(), players: 1);
        world.CreateSeat("p0");
        return world;
    }

    /// <summary>Reads the board each time it is asked, and remembers what it saw.</summary>
    private sealed class Watcher(World world) : NoCardAbilities
    {
        private readonly Dictionary<WindowKind, int> active = [];

        public List<Occurrence> Seen { get; } = [];

        public int Windows => Seen.Count;

        public int ActiveAt(WindowKind window) => active[window];

        public override IReadOnlyList<PendingAbility> Waiting(
            World asked, Occurrence occurrence, WindowKind window)
        {
            Seen.Add(occurrence);
            active[window] = world.Effects.Active().Count;
            return [];
        }

        public override IReadOnlyList<GameEvent> Resolve(
            World asked, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];

        public override Affordance Describe(World asked, PendingAbility ability) =>
            new(ability.Card, "Use", ability.Card, ability.Player, $"ability on {ability.Card}");
    }

    /// <summary>Puts a fixed set of abilities into every window.</summary>
    private sealed class Offering(params PendingAbility[] abilities) : NoCardAbilities
    {
        public int Resolved { get; private set; }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) => abilities;

        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
        {
            Resolved += 1;
            return [];
        }

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(ability.Card, "Use", ability.Card, ability.Player, $"ability on {ability.Card}");
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}
