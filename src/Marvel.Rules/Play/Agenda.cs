using Marvel.Rules.Timing;
using Marvel.Rules.State;

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
/// <param name="Character">
/// The character an attack is against, or <c>-1</c> for the attacked player's
/// identity. <c>rr:attack-enemy-activation.1.1</c>: "normally the attacked
/// character is the player's hero, but abilities can instead cause an enemy to
/// attack a player's alter-ego or <b>an ally that player controls</b>", and
/// <c>rr:attacks-against-allies.1</c> keeps the player attacked either way. So
/// this names a character and not a second seat.
/// </param>
/// <param name="Tier">
/// Which of a card's abilities suspended here, or null for a step that is not
/// an ability waiting on an answer.
/// <para>
/// Only <c>Steps.ChooseOption</c> carries one. A suspended ability is found
/// again from its card, because a step cannot hold an effect tree — and a card
/// with a choice in two of its abilities cannot be found again from the card
/// and a position alone. Infinite Hunter is the first: a "When Revealed" that
/// chooses an ally and a "Boost" that chooses between two effects.
/// </para>
/// </param>
public readonly record struct PhaseStep(
    string What, int Round, int Number, int Index = 0, int Subject = -1, int Seat = -1,
    bool Plan = false, int Character = -1, Timing.AbilityType? Tier = null)
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
    public Occurrence OccurrenceOf(World world, ICardFacts facts)
    {
        int id = Moment.Id(Round, Number, Index);

        return What switch
        {
            Steps.Attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                Subject,
                Character >= 0 ? Character : world.Seats[Seat].IdentityCard.ObjectId,
                Seat),
            Steps.CharacterAttacks when world.CharacterAttack is { } attack =>
                Occurrence.ForAttack(
                    id,
                    Conditions,
                    world,
                    facts,
                    attack.Attacker,
                    attack.Enemy),
            Steps.CharacterThwarts when world.CharacterThwart is { } thwart =>
                Occurrence.ForThwart(
                    id,
                    Conditions,
                    world,
                    facts,
                    thwart.Thwarter,
                    thwart.Scheme,
                    thwart.Player),
            Steps.DealAttackDamage when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player),
            Steps.EndAttack when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player),
            _ => new Occurrence(id, Conditions, Subject, Seat),
        };
    }

    /// <summary>An occurrence that needs no live attack roles, or null.</summary>
    public Occurrence? ScheduledOccurrence => What is
        Steps.Attack or Steps.CharacterAttacks or Steps.CharacterThwarts or Steps.EndAttack
            ? null
            : new Occurrence(Moment.Id(Round, Number, Index), Conditions, Subject, Seat);
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
    private readonly List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> items = [];
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

    /// <summary>Create the current occurrence once, from the board it begins on.</summary>
    /// <remarks>
    /// Scheduling can precede an occurrence by several questions. In
    /// particular, declaring a defender changes an attack's target before its
    /// damage occurrence begins. Capturing here gets the target at the start of
    /// the interrupt window and keeps it stable for the rest of that window.
    /// </remarks>
    public Occurrence Begin(World world, ICardFacts facts)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("the agenda has no current occurrence");
        }

        var (step, stage, occurrence) = items[0];
        if (occurrence is null
            || (step.What == Steps.DealAttackDamage
                && occurrence.Actor < 0
                && world.Attack is not null))
        {
            occurrence = step.OccurrenceOf(world, facts);
        }
        items[0] = (step, stage, occurrence);
        return occurrence;
    }

    /// <summary>Every outstanding step, in the order they will be taken.</summary>
    public IReadOnlyList<PhaseStep> Outstanding => [.. items.Select(item => item.Step)];

    /// <summary>Put a step at the end of the list.</summary>
    /// <param name="step">What to do.</param>
    public void Add(PhaseStep step) =>
        items.Add((step, Stage.Interrupts, step.ScheduledOccurrence));

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
        items.Insert(
            Math.Min(scheduled, items.Count),
            (step, Stage.Interrupts, step.ScheduledOccurrence));
    }

    /// <summary>
    /// Schedule a step to be taken <i>before</i> the current one happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:interrupt.1</c>: an interrupt "resolves <b>before</b> the
    /// triggering condition". For an interrupt whose effect is itself an
    /// activation that is not enough on its own, because
    /// <c>rr:activation.8</c> would otherwise put the new activation after —
    /// "an activation initiated during another resolves after the current
    /// activation has finished resolving". Speed Demon prints the exception
    /// as a reminder: "<i>(Resolve Speed Demon's attack first.)</i>"
    /// </para>
    /// <para>
    /// The step it goes in front of keeps the stage it had reached, so the
    /// interrupt window that was open re-opens when the agenda comes back to
    /// it. That is <c>rr:interrupt.5</c> and not an accident: using an
    /// interrupt "gives each player another opportunity" to use one.
    /// </para>
    /// </remarks>
    /// <param name="step">What to do first.</param>
    public void Now(PhaseStep step)
    {
        items.Insert(0, (step, Stage.Interrupts, step.ScheduledOccurrence));

        // The inserted step is where `Then` now counts from, and it has
        // scheduled nothing of its own yet.
        scheduled = 0;
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
    /// <c>rr:scheme-enemy-activation</c>. Steps 1 and 2: the boost card.
    /// </summary>
    public const string Scheme = "Scheme";

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A step of its own because step 2 can stop and ask.</b> "Resolve each
    /// of the scheming enemy's boost cards" is step 2 and "place threat on the
    /// main scheme equal to the scheming enemy's modified SCH value" is step 3,
    /// in that order — and a <b>Boost</b> ability that offers the player a
    /// choice suspends. Resolved inline, the threat went on the scheme while
    /// the question was still on the table, and whatever the player chose
    /// arrived too late to count.
    /// </para>
    /// <para>
    /// The attack activation has the same shape:
    /// <see cref="FlipBoostCards"/> is step 3 and
    /// <see cref="CalculateAttackDamage"/> is step 4, so a boost card's
    /// question is answered between them. This is the same split one
    /// activation over.
    /// </para>
    /// <para>
    /// <b>It is also where a scheme activation ends</b>, so it carries
    /// <see cref="SchemeEnds"/> — the parallel of <see cref="AttackEnds"/> on
    /// <see cref="EndAttack"/>. "After [enemy] schemes" is a claim about the
    /// activation being over, and <c>rr:activation.6</c> is where it is over.
    /// </para>
    /// <para>
    /// It does not carry <c>WhenThreatPlaced</c>. That is
    /// <see cref="PlaceThreat"/>'s, and <see cref="PlaceThreat"/> is villain
    /// phase step 1 — a different moment. Hunting Gene Traitors answers "after
    /// resolving step one of the villain phase" and must not fire again every
    /// time the villain schemes.
    /// </para>
    /// </remarks>
    public const string SchemeThreat = "SchemeThreat";

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
    /// Step 4 of an attack — <c>rr:attack-enemy-activation.step.4</c>.
    /// The calculated amount is saved on the attack for the next step.
    /// </summary>
    public const string CalculateAttackDamage = "CalculateAttackDamage";

    /// <summary>
    /// Step 5 of an attack — <c>rr:attack-enemy-activation.step.5</c>.
    /// This deals the amount fixed by <see cref="CalculateAttackDamage"/>.
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
    /// A hero or ally thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// A step for the reason <see cref="CharacterAttacks"/> is one, arrived at
    /// from the other end. <c>rr:thwart</c> lists no steps of its own, but
    /// <c>rr:consequential-damage.1</c> deals an ally's consequential damage
    /// "after resolving abilities that are triggered by the ally attacking
    /// <b>or thwarting</b>" — so the rules take it for granted that a thwart
    /// has abilities triggered by it, and abilities triggered by something are
    /// abilities in its windows.
    /// </remarks>
    public const string CharacterThwarts = "CharacterThwarts";

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

    /// <summary>
    /// An ally's consequential damage after a thwart —
    /// <c>rr:consequential-damage.1</c>.
    /// </summary>
    /// <remarks>
    /// The same rule as <see cref="AllyConsequentialDamage"/> and a separate
    /// step only because the two differ in what they record: an ally that
    /// thwarted takes its damage under the verb "Thwart", and the event stream
    /// is how a reader tells the two apart. Which <i>field</i> was used is a
    /// third question and not this one — <c>rr:assault.2</c> makes a thwart
    /// against an assaulted scheme take the damage printed under ATK.
    /// </remarks>
    public const string AllyThwartConsequentialDamage = "AllyThwartConsequentialDamage";

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
    /// An attack begins, whoever its actor and target are.
    /// </summary>
    /// <remarks>
    /// Enemy attacks use the timing in <c>rr:attack-enemy-activation.5</c>.
    /// Character attacks use <c>rr:attack-player-ability-type.step.7</c> and
    /// <c>.step.8</c>. The occurrence's actor and target roles distinguish the
    /// printed cases without source-specific condition names.
    /// </remarks>
    public const string AttackInitiated = "WhenAttackInitiated";

    /// <summary>
    /// "When an enemy schemes" — <c>rr:scheme-enemy-activation</c>. The
    /// <i>start</i> of the activation, which is what an interrupt to it means.
    /// </summary>
    public const string EnemySchemes = "WhenEnemySchemes";

    /// <summary>
    /// "After [enemy] schemes" — the end of a scheme activation,
    /// <c>rr:activation.6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parallel of <see cref="AttackEnds"/>, and separate from
    /// <see cref="EnemySchemes"/> for the same reason the attack keeps its two
    /// apart: <c>rr:attack-enemy-activation.5</c> puts "when [enemy name]
    /// attacks" at the moment the attack is <i>initiated</i>, before any of its
    /// steps, and <c>.step.6.a</c> is where the abilities that ask what the
    /// attack <i>did</i> live. A scheme has the same two moments and had only
    /// one name for them.
    /// </para>
    /// <para>
    /// It matters because the threat is placed in between. Prelate Armor's
    /// "<b>Forced Response</b>: After Unus schemes, give him a tough status
    /// card" resolved at the start of the activation while the two steps were
    /// one call, and nothing showed it — a tough card is a tough card whichever
    /// side of the scheme it lands on. The event order is what shows it.
    /// </para>
    /// </remarks>
    public const string SchemeEnds = "WhenSchemeEnds";

    /// <summary>"When an attack ends" — <c>rr:attack-enemy-activation.step.6</c>.</summary>
    public const string AttackEnds = "WhenAttackEnds";

    /// <summary>"When a card is revealed" — <c>rr:reveal</c>.</summary>
    public const string CardRevealed = "WhenCardRevealed";

    /// <summary>
    /// Resolving setup's abilities — <c>rr:appendix-ii-setup.step.12</c>.
    /// </summary>
    /// <remarks>
    /// <b>Not a triggering condition</b>, and deliberately absent from
    /// <see cref="EveryCondition"/>: <c>rr:setup-triggered-ability.2</c> times a
    /// "Setup" ability to a step of setup rather than to something happening,
    /// and setup is not on the agenda. This is the label its events carry, so
    /// that a board built during setup can be told apart in the stream from one
    /// built during a round.
    /// </remarks>
    public const string Setup = "Setup";

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

    /// <summary>A character was dealt damage.</summary>
    public const string DamageDealt = "WhenDamageDealt";

    /// <summary>
    /// A character whose remaining hit points have reached zero is about to be
    /// defeated — <c>rr:damage.step.6</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="CardDefeated"/>: <c>rr:would</c> gives this
    /// condition higher priority, and an interrupt that changes the imminent
    /// defeat prevents the later condition from occurring.
    /// </remarks>
    public const string CardWouldBeDefeated = "WhenCardWouldBeDefeated";

    /// <summary>A player card has finished entering play.</summary>
    public const string CardPlayed = "WhenCardPlayed";

    /// <summary>A card being defeated — <c>rr:defeat</c>.</summary>
    /// <remarks>
    /// <para>
    /// A condition rather than a step, and <c>rr:triggering-condition.2</c> is
    /// why: "a single attack causing a character to both take damage and be
    /// defeated" gets "a single interrupt window and a single response window",
    /// so the defeat joins the occurrence that caused it instead of being
    /// scheduled beside it. <c>Occurrence.Also</c> is where it joins.
    /// </para>
    /// <para>
    /// <b>Reachable in a response window, and not in an interrupt one.</b> Not
    /// a gap: <c>rr:damage.step.7</c> puts "abilities that trigger <i>when
    /// [character] is defeated…</i>" after <c>.step.5</c> has placed the
    /// damage, which is past the window. So the interrupt tier is reached from
    /// inside the damage — <c>ICardAbilities.WhenCardDefeated</c> — and every
    /// ability there is forced, with nothing to offer and nothing to decline.
    /// The response tier is <c>.step.9</c>, which is the window.
    /// </para>
    /// <para>
    /// <c>rr:damage.step.6</c> is a different condition:
    /// <see cref="CardWouldBeDefeated"/>. It happens after damage is placed
    /// and before this condition, so a replacement there can prevent this one.
    /// </para>
    /// </remarks>
    public const string CardDefeated = "WhenCardDefeated";

    /// <summary>
    /// A character thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Occurrence.Subject"/> is the scheme, so a card on it answers
    /// with <c>this</c>, and <see cref="Occurrence.Player"/> is the seat
    /// thwarting, so a player card answers with <c>you</c>.
    /// </remarks>
    public const string CharacterThwartsScheme = "WhenCharacterThwarts";

    private static readonly Dictionary<string, string[]> Conditions = new(StringComparer.Ordinal)
    {
        [PlaceThreat] = ["WhenThreatPlaced"],

        // Two conditions at one moment again: an attack *is* an activation
        // (`rr:activation`, "whenever an enemy attacks or schemes, it is
        // considered to have activated"), so both are true of the same
        // occurrence and `rr:triggering-condition.2` gives them one window
        // pair between them.
        [Attack] = [EnemyActivates, AttackInitiated],
        [Scheme] = [EnemyActivates, EnemySchemes],
        [SchemeThreat] = [SchemeEnds],
        [GiveBoostCard] = ["WhenBoostCardGiven"],
        [DeclareDefender] = ["WhenDefenderDeclared"],
        [FlipBoostCards] = ["WhenBoostCardsFlipped"],
        // Damage from an attack is imminent before this step applies and dealt
        // after it applies. `rr:triggering-condition.2` gives one occurrence
        // one pair of windows when it creates several conditions.
        [DealAttackDamage] = [DamageWouldBeDealt, DamageDealt],
        [EndAttack] = [AttackEnds],
        [DealEncounterCards] = ["WhenEncounterCardsDealt"],
        [RevealEncounterCard] = [CardRevealed],
        [TurnAction] = [TurnAction],
        [CardDefeated] = [CardDefeated],
        [CharacterAttacks] = [AttackInitiated],
        [CharacterThwarts] = [CharacterThwartsScheme],
        [DamageWouldBeDealt] = [DamageWouldBeDealt],
        [CardWouldBeDefeated] = [CardWouldBeDefeated],
        [CardPlayed] = [CardPlayed],
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
