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
/// <param name="Subject">The object id it acts on, or <c>-1</c>.</param>
/// <param name="Seat">
/// The player it concerns, or <c>-1</c> for a step that concerns nobody in
/// particular. Separate from <paramref name="Index"/>, which only has to make
/// repetitions of a step distinct: threat is placed once per round and concerns
/// no player, and reading its index as a seat would tell every card that it
/// happened to the first one.
/// </param>
/// <param name="Plan">
/// Whether this only schedules other steps. A plan is not an occurrence, so it
/// opens no windows: <c>rr:villain-phase.step.2</c> is a heading, and the
/// activations under it are the things that happen.
/// </param>
public readonly record struct PhaseStep(
    string What, int Round, int Number, int Index = 0, int Subject = -1, int Seat = -1,
    bool Plan = false)
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
    public Occurrence Occurrence =>
        new(Moment.Id(Round, Number, Index), Conditions, Subject, Seat);
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
    private readonly List<(PhaseStep Step, Stage Stage, Occurrence Occurrence)> items = [];
    private int scheduled;

    /// <summary>Whether the game is part-way through anything.</summary>
    public bool IsBusy => items.Count > 0;

    /// <summary>How many steps are outstanding.</summary>
    public int Count => items.Count;

    /// <summary>The step being worked on.</summary>
    public PhaseStep? Current => items.Count > 0 ? items[0].Step : null;

    /// <summary>Which part of it.</summary>
    public Stage Stage => items.Count > 0 ? items[0].Stage : Stage.Apply;

    /// <summary>
    /// What is happening, as one occurrence that lasts the whole step.
    /// </summary>
    /// <remarks>
    /// <b>Made once, when the step is scheduled, and not on every read.</b>
    /// <c>rr:triggering-condition.1</c> lets each ability trigger once per
    /// occurrence, and an occurrence is what remembers which have. A fresh one
    /// per read would forget across the answer that suspended the step, and the
    /// forced interrupt that had just resolved would resolve again — and again.
    /// </remarks>
    public Occurrence? Occurrence => items.Count > 0 ? items[0].Occurrence : null;

    /// <summary>Every outstanding step, in the order they will be taken.</summary>
    public IReadOnlyList<PhaseStep> Outstanding => [.. items.Select(item => item.Step)];

    /// <summary>Put a step at the end of the list.</summary>
    /// <param name="step">What to do.</param>
    public void Add(PhaseStep step) => items.Add((step, Stage.Interrupts, step.Occurrence));

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
        items.Insert(Math.Min(scheduled, items.Count), (step, Stage.Interrupts, step.Occurrence));
    }

    /// <summary>Move the current step on to its next part.</summary>
    /// <returns>False when the step is finished and has been taken off the list.</returns>
    public bool Advance()
    {
        var (step, stage, occurrence) = items[0];
        switch (stage)
        {
            case Stage.Interrupts:
                items[0] = (step, Stage.Apply, occurrence);
                return true;

            case Stage.Apply:
                items[0] = (step, Stage.Responses, occurrence);
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

    /// <summary>
    /// One enemy attacking one player — <c>rr:activation.1</c>,
    /// <c>rr:attack-enemy-activation</c>.
    /// </summary>
    public const string Attack = "Attack";

    /// <summary>
    /// One enemy scheming — <c>rr:activation.1</c>,
    /// <c>rr:scheme-enemy-activation</c>.
    /// </summary>
    public const string Scheme = "Scheme";

    /// <summary>
    /// Step 1 of an attack — <c>rr:attack-enemy-activation.step.1</c>.
    /// </summary>
    public const string GiveBoostCard = "GiveBoostCard";

    /// <summary>
    /// Step 2 of an attack — <c>rr:attack-enemy-activation.step.2</c>.
    /// </summary>
    public const string DeclareDefender = "DeclareDefender";

    /// <summary>
    /// Step 3 of an attack — <c>rr:attack-enemy-activation.step.3</c>.
    /// </summary>
    public const string FlipBoostCards = "FlipBoostCards";

    /// <summary>
    /// Steps 4 and 5 of an attack — <c>rr:attack-enemy-activation.step.4</c>
    /// and <c>.step.5</c>. One step because <c>rr:triggering-condition.2</c>
    /// makes calculating and dealing one occurrence: nothing can happen between
    /// the amount being fixed and it being dealt.
    /// </summary>
    public const string DealAttackDamage = "DealAttackDamage";

    /// <summary>
    /// Step 6 of an attack — <c>rr:attack-enemy-activation.step.6</c>.
    /// </summary>
    public const string EndAttack = "EndAttack";

    /// <summary>
    /// A hero or ally attacking an enemy —
    /// <c>rr:attack-player-ability-type</c>.
    /// </summary>
    /// <remarks>
    /// A step and not a call, for the reason every other attack is: `.step.7`
    /// and `.step.8` put abilities around it — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked" —
    /// and an ability may ask the player something. A basic attack that
    /// resolved inline had nowhere to open those windows.
    /// </remarks>
    public const string CharacterAttacks = "CharacterAttacks";

    /// <summary>
    /// An ally's consequential damage —
    /// <c>rr:attack-player-ability-type.step.9</c>.
    /// </summary>
    /// <remarks>
    /// Last of the steps an attack's resolution runs, after the forced and
    /// non-forced abilities of <c>.step.7</c> and <c>.step.8</c> —
    /// <c>rr:consequential-damage.1</c> says the same thing the other way
    /// round, "after resolving abilities that are triggered by the ally
    /// attacking or thwarting". A step of its own because those abilities are
    /// windows and a window can ask.
    /// </remarks>
    public const string AllyConsequentialDamage = "AllyConsequentialDamage";

    /// <summary>Step 3 — <c>rr:villain-phase.step.3</c>.</summary>
    public const string DealEncounterCards = "DealEncounterCards";

    /// <summary>
    /// Step 4 — <c>rr:villain-phase.step.4</c>. A heading, and a loop.
    /// </summary>
    /// <remarks>
    /// "Each player repeats this process in player order, <b>until no dealt
    /// encounter cards remain</b>." So this step does not hand out a list of
    /// reveals; it finds the next card, schedules that one reveal, and puts
    /// itself back on the agenda. A card revealed here that deals another card
    /// has that card revealed here too — <c>rr:deal-deal-an-encounter-card.1</c>.
    /// </remarks>
    public const string RevealEncounterCards = "RevealEncounterCards";

    /// <summary>One card being revealed — <c>rr:reveal</c>, <c>rr:villain-phase.step.4</c>.</summary>
    public const string RevealEncounterCard = "RevealEncounterCard";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    /// <summary>
    /// A card ability waiting for a player to choose between its options —
    /// <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// A step rather than a call for the same reason an attack is one: the
    /// ability has to stop and ask, and an interpreter that returns a list of
    /// events has nowhere to stop. What suspends is the ability; what resumes
    /// it is the answer to this.
    /// </remarks>
    public const string ChooseOption = "ChooseOption";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    public const string PassFirstPlayerToken = "PassFirstPlayerToken";

    /// <summary>Step 6 — <c>rr:villain-phase.step.6</c>.</summary>
    public const string EndVillainPhase = "EndVillainPhase";

    /// <summary>
    /// Step 2 — <c>rr:end-of-player-phase.step.2</c>.
    /// </summary>
    /// <remarks>
    /// "Each player <b>simultaneously</b> draws up to their hand size", so one
    /// step for the table rather than one per player. Step 1 is the opposite —
    /// it is "in player order" — and lives on the turn prompt, because it is a
    /// question rather than something that happens.
    /// </remarks>
    public const string DrawToHandSize = "DrawToHandSize";

    /// <summary>
    /// Step 3 — <c>rr:end-of-player-phase.step.3</c>. Simultaneous, as step 2 is.
    /// </summary>
    public const string ReadyCards = "ReadyCards";

    /// <summary>The end of the player phase — <c>rr:end-of-player-phase</c>.</summary>
    public const string EndPlayerPhase = "EndPlayerPhase";

    /// <summary>"Whenever an enemy attacks or schemes" — <c>rr:activation</c>.</summary>
    public const string EnemyActivates = "WhenEnemyActivates";

    /// <summary>
    /// "When the villain initiates an attack" — <c>rr:attack-enemy-activation.5</c>,
    /// which says an interrupt triggering "when [enemy name] attacks" has this
    /// same timing.
    /// </summary>
    public const string EnemyAttacks = "WhenEnemyAttacks";

    /// <summary>"When an enemy schemes" — <c>rr:scheme-enemy-activation</c>.</summary>
    public const string EnemySchemes = "WhenEnemySchemes";

    /// <summary>"When an attack ends" — <c>rr:attack-enemy-activation.step.6</c>.</summary>
    public const string AttackEnds = "WhenAttackEnds";

    /// <summary>"When a card is revealed" — <c>rr:reveal</c>.</summary>
    public const string CardRevealed = "WhenCardRevealed";

    /// <summary>
    /// A player triggering an "Action" ability on their turn —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// A condition rather than a step: an action is not scheduled, it is one of
    /// the six things a turn offers and it happens when the player says so. It
    /// is here so that a card can answer "after a player triggers an action",
    /// and so that <see cref="EveryCondition"/> knows the name.
    /// </remarks>
    public const string TurnAction = "WhenActionTriggered";

    /// <summary>
    /// Damage about to be dealt to a character —
    /// <c>rr:damage.step.1</c>.
    /// </summary>
    /// <remarks>
    /// The first of the nine steps <c>rr:damage</c> lists: "abilities that
    /// trigger <i>when [character] would deal/be dealt any amount of
    /// damage</i>". This is the "be dealt" half; the dealer's half is the same
    /// step and nothing in the pool that the engine reaches uses it yet.
    /// </remarks>
    public const string DamageWouldBeDealt = "WhenDamageWouldBeDealt";

    /// <summary>A card being defeated — <c>rr:defeat</c>.</summary>
    /// <remarks>
    /// A condition rather than a step: a defeat happens inside dealing damage
    /// or removing threat, wherever the rules put it, and not on the agenda.
    /// It is here so that <c>rr:when-defeated-abilities</c> has a condition to
    /// name.
    /// </remarks>
    public const string CardDefeated = "WhenCardDefeated";

    /// <summary>
    /// A character attacking an enemy —
    /// <c>rr:attack-player-ability-type.step.7</c>.
    /// </summary>
    /// <remarks>
    /// One condition for both printed shapes, because the occurrence carries
    /// both ends: <see cref="Occurrence.Subject"/> is the enemy being attacked,
    /// so a card on that enemy answers with <c>this</c> — Shocker's "after
    /// Shocker is attacked" — and <see cref="Occurrence.Player"/> is the seat
    /// attacking, so a player card answers with <c>you</c>. Two conditions
    /// would need two subjects and there is only one subject field.
    /// </remarks>
    public const string CharacterAttacksEnemy = "WhenCharacterAttacks";

    private static readonly Dictionary<string, string[]> Conditions = new(StringComparer.Ordinal)
    {
        [PlaceThreat] = ["WhenThreatPlaced"],

        // Two conditions at one moment again: an attack *is* an activation
        // (`rr:activation`, "whenever an enemy attacks or schemes, it is
        // considered to have activated"), so both are true of the same
        // occurrence and `rr:triggering-condition.2` gives them one window
        // pair between them.
        [Attack] = [EnemyActivates, EnemyAttacks],
        [Scheme] = [EnemyActivates, EnemySchemes],
        [GiveBoostCard] = ["WhenBoostCardGiven"],
        [DeclareDefender] = ["WhenDefenderDeclared"],
        [FlipBoostCards] = ["WhenBoostCardsFlipped"],
        [DealAttackDamage] = ["WhenDamageDealt"],
        [EndAttack] = [AttackEnds],
        [DealEncounterCards] = ["WhenEncounterCardsDealt"],
        [RevealEncounterCard] = [CardRevealed],
        [TurnAction] = [TurnAction],
        [CardDefeated] = [CardDefeated],
        [CharacterAttacks] = [CharacterAttacksEnemy],
        [DamageWouldBeDealt] = [DamageWouldBeDealt],
        [ChooseOption] = ["WhenOptionChosen"],
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

    /// <summary>
    /// Every triggering condition any step in this engine produces.
    /// </summary>
    /// <remarks>
    /// Derived from the table above rather than listed again, so that it cannot
    /// fall behind it. What it is for: an authored card names the condition it
    /// answers, and a card naming one nothing ever produces would sit in the
    /// dataset looking implemented and never fire. Holding the two sets against
    /// each other turns that into a failing test.
    /// </remarks>
    public static IReadOnlySet<string> EveryCondition { get; } =
        new HashSet<string>(Conditions.Values.SelectMany(each => each), StringComparer.Ordinal);
}
