using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>
public enum Stage
{
    /// <summary>Before it happens — <c>rr:ability.step.2</c>.</summary>
    Interrupts,

    /// <summary>It happens — <c>rr:ability.step.3</c>.</summary>
    Apply,

    /// <summary>After it happened — <c>rr:ability.step.4</c>.</summary>
    Responses,
}

/// <summary>
/// One thing the game is going to do, not yet done.
/// </summary>
/// <param name="What">Which step, from <see cref="Steps"/>.</param>
/// <param name="Round">Which round it belongs to.</param>
/// <param name="Number">The Rules Reference's number for it within its phase.</param>
/// <param name="Index">Which repetition — which player, or which dealt card.</param>
/// <param name="Subject">The object id it acts on, where it has one.</param>
/// <param name="Plan">
/// Whether this only schedules other steps. A plan is not an occurrence, so it
/// opens no windows: <c>rr:villain-phase.step.2</c> is a heading, and the
/// activations under it are the things that happen.
/// </param>
public readonly record struct PhaseStep(
    string What, int Round, int Number, int Index = 0, int Subject = 0, bool Plan = false)
{
    /// <summary>What is happening, as triggering conditions.</summary>
    /// <remarks>
    /// Usually one. The villain phase's ending is two — the phase ends and the
    /// round ends — and <c>rr:triggering-condition.2</c> is why they share one
    /// occurrence rather than getting two windows each.
    /// </remarks>
    public IReadOnlyList<string> Conditions => Steps.ConditionsOf(What);

    /// <summary>This step's occurrence, distinct from every other in the game.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.1</c> is per occurrence, so two threat
    /// placements in the same game must not share an id — the second would find
    /// every interrupt already spent.
    /// </remarks>
    public Occurrence Occurrence => new(Moment.Id(Round, Number, Index), Conditions);
}

/// <summary>
/// What the game still has to do, and where in it the game is.
/// </summary>
/// <remarks>
/// <para>
/// A phase is not a call. It is a list of steps on the board, each part-way
/// through <see cref="Stage"/>, and the engine walks it until something needs a
/// player's answer. That is the only shape that lets the game stop in the middle
/// of the villain phase — which it must, because <c>rr:ability</c> puts a window
/// before and after every occurrence and any of them may hold an ability
/// somebody has to be asked about.
/// </para>
/// <para>
/// Data, so it can be written to a save. The alternative is a suspended call
/// stack, which cannot be saved, cannot be diffed against a recorded step, and
/// cannot tell a client what the game is waiting for.
/// </para>
/// <para>
/// It also makes <c>rr:villain-phase</c>'s six steps <b>visible</b>. They used
/// to be the order of six method calls, which is a thing a reader has to
/// reconstruct; now they are six values that can be listed.
/// </para>
/// </remarks>
public sealed class Agenda
{
    private readonly List<(PhaseStep Step, Stage Stage)> items = [];
    private int scheduled;

    /// <summary>Whether the game is part-way through anything.</summary>
    public bool IsBusy => items.Count > 0;

    /// <summary>How many steps are outstanding.</summary>
    public int Count => items.Count;

    /// <summary>The step being worked on.</summary>
    public PhaseStep? Current => items.Count > 0 ? items[0].Step : null;

    /// <summary>Which part of it.</summary>
    public Stage Stage => items.Count > 0 ? items[0].Stage : Stage.Apply;

    /// <summary>Every outstanding step, in the order they will be taken.</summary>
    public IReadOnlyList<PhaseStep> Outstanding => [.. items.Select(item => item.Step)];

    /// <summary>Put a step at the end of the list.</summary>
    /// <param name="step">What to do.</param>
    public void Add(PhaseStep step) => items.Add((step, Stage.Interrupts));

    /// <summary>
    /// Schedule a step to be taken as soon as the current one is finished with.
    /// </summary>
    /// <remarks>
    /// After the current step's <i>response</i> window, not before it: a step
    /// that schedules another has not itself finished happening.
    /// <c>rr:villain-phase.step.3</c> deals the encounter cards and
    /// <c>.step.4</c> reveals them, in that order and not interleaved.
    /// </remarks>
    /// <param name="step">What to do next.</param>
    public void Then(PhaseStep step)
    {
        scheduled += 1;
        items.Insert(Math.Min(scheduled, items.Count), (step, Stage.Interrupts));
    }

    /// <summary>Move the current step on to its next part.</summary>
    /// <returns>False when the step is finished and has been taken off the list.</returns>
    public bool Advance()
    {
        var (step, stage) = items[0];
        switch (stage)
        {
            case Stage.Interrupts:
                items[0] = (step, Stage.Apply);
                return true;

            case Stage.Apply:
                items[0] = (step, Stage.Responses);
                return true;

            default:
                items.RemoveAt(0);
                scheduled = 0;
                return false;
        }
    }

    /// <summary>
    /// Abandon everything outstanding.
    /// </summary>
    /// <remarks>
    /// For the end of the game. <c>rr:winning-the-game</c> and
    /// <c>rr:main-scheme-main-scheme-deck.2.1</c> both end it outright, and the
    /// rest of the villain phase does not happen.
    /// </remarks>
    public void Abandon()
    {
        items.Clear();
        scheduled = 0;
    }
}

/// <summary>The steps this engine knows how to take.</summary>
/// <remarks>
/// Named after the Rules Reference's own steps, so a divergence can be argued
/// against the published text rather than against a call graph.
/// </remarks>
public static class Steps
{
    /// <summary>The villain phase, which schedules its six steps.</summary>
    public const string VillainPhase = "VillainPhase";

    /// <summary>Step 1 — <c>rr:villain-phase.step.1</c>.</summary>
    public const string PlaceThreat = "PlaceThreat";

    /// <summary>Step 2, a heading — <c>rr:villain-phase.step.2</c>.</summary>
    public const string EnemiesActivate = "EnemiesActivate";

    /// <summary>One enemy activating against one player — <c>rr:activation.1</c>.</summary>
    public const string Activate = "Activate";

    /// <summary>Step 3 — <c>rr:villain-phase.step.3</c>.</summary>
    public const string DealEncounterCards = "DealEncounterCards";

    /// <summary>One card being revealed — <c>rr:reveal</c>, <c>rr:villain-phase.step.4</c>.</summary>
    public const string RevealEncounterCard = "RevealEncounterCard";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    public const string PassFirstPlayerToken = "PassFirstPlayerToken";

    /// <summary>Step 6 — <c>rr:villain-phase.step.6</c>.</summary>
    public const string EndVillainPhase = "EndVillainPhase";

    /// <summary>The end of the player phase — <c>rr:end-of-player-phase</c>.</summary>
    public const string EndPlayerPhase = "EndPlayerPhase";

    private static readonly Dictionary<string, string[]> Conditions = new(StringComparer.Ordinal)
    {
        [PlaceThreat] = ["WhenThreatPlaced"],
        [Activate] = ["WhenEnemyActivates"],
        [DealEncounterCards] = ["WhenEncounterCardsDealt"],
        [RevealEncounterCard] = ["WhenCardRevealed"],
        [PassFirstPlayerToken] = ["WhenFirstPlayerTokenPassed"],

        // Two conditions at one moment, because `rr:villain-phase.step.6` is
        // titled "End of Villain Phase and Round" and both are reached there.
        // `rr:triggering-condition.2` gives them one interrupt window and one
        // response window between them.
        [EndVillainPhase] = [PhaseEnd.VillainPhaseEnds, PhaseEnd.RoundEnds],
        [EndPlayerPhase] = [PhaseEnd.PlayerPhaseEnds],
    };

    /// <summary>The triggering conditions a step creates.</summary>
    /// <param name="what">One of the step names here.</param>
    public static IReadOnlyList<string> ConditionsOf(string what) =>
        Conditions.TryGetValue(what, out var conditions) ? conditions : [what];
}
