using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>

/// <summary>
/// One thing the game is going to do, not yet done.
/// </summary>
/// <para><c>What</c> — Which step, from <see cref="Steps"/>.</para>
/// <para><c>Round</c> — Which round it belongs to.</para>
/// <para><c>Number</c> — The Rules Reference's number for it within its phase.</para>
/// <para><c>Index</c> — Which repetition — which player, or which dealt card.</para>
/// <para><c>Subject</c> — The object id it acts on, or <c>-1</c>.</para>
/// <para><c>Seat</c> —
/// The player it concerns, or <c>-1</c> for a step that concerns nobody in
/// particular. Separate from <c>Index</c>, which only has to make
/// repetitions of a step distinct: threat is placed once per round and concerns
/// no player, and reading its index as a seat would tell every card that it
/// happened to the first one.
/// </para>
/// <para><c>Plan</c> —
/// Whether this only schedules other steps. A plan is not an occurrence, so it
/// opens no windows: <c>rr:villain-phase.step.2</c> is a heading, and the
/// activations under it are the things that happen.
/// </para>
/// <para><c>Character</c> —
/// The character an attack is against, or <c>-1</c> for the attacked player's
/// identity. <c>rr:attack-enemy-activation.1.1</c>: "normally the attacked
/// character is the player's hero, but abilities can instead cause an enemy to
/// attack a player's alter-ego or <b>an ally that player controls</b>", and
/// <c>rr:attacks-against-allies.1</c> keeps the player attacked either way. So
/// this names a character and not a second seat.
/// </para>
/// <para><c>Tier</c> —
/// Which of a card's abilities suspended here, or null for a step that is not
/// an ability waiting on an answer.
/// <para>
/// Only <c>Steps.ChooseOption</c> carries one. A suspended ability is found
/// again from its card, because a step cannot hold an effect tree — and a card
/// with a choice in two of its abilities cannot be found again from the card
/// and a position alone. Infinite Hunter is the first: a "When Revealed" that
/// chooses an ally and a "Boost" that chooses between two effects.
/// </para>
/// </para>
/// <para><c>Placement</c> —
/// A threat assignment already known when the step was scheduled, or null for
/// a step whose assignment is derived when its interrupt window begins.
/// </para>
/// <para><c>ActivationId</c> —
/// The stable identity of the enemy activation this step belongs to, or -1.
/// The spelling and allocation are engine choices; the rules require only that
/// a nested activation wait for the current one to finish.
/// </para>
/// <para><c>FinalStep</c> —
/// Whether this ability is the final step of the card-defined sequence that
/// scheduled it. The rulebook defines sequences but does not choose this field's
/// spelling; it is engine data carried so a suspended Special can resume with
/// the same answer.
/// </para>
/// <para><c>FinalPlayer</c> —
/// Whether this is the last frame of an effect that resolves once for each
/// player. The chosen order is represented by the frames themselves; this flag
/// lets the card interpreter resume any outer sequence after the final frame
/// without retaining a live iterator or effect tree.
/// </para>
/// <para><c>EachPlayerFrame</c> —
/// Whether a suspended choice belongs to one player's frame of an each-player
/// effect. Together with <c>FinalPlayer</c>, this tells the card
/// interpreter whether answering the choice resumes only this player's body or
/// also the outer sequence. The spelling is an engine continuation choice.
/// </para>
/// <para><c>Trigger</c> —
/// Event-stream provenance carried by an internal continuation, or empty.
/// The spelling is an engine choice rather than a Rules Reference term.
/// </para>
/// <para><c>CharacterAttack</c> — The complete queued player attack, when this step is one.</para>
/// <para><c>CharacterThwart</c> — The complete queued player thwart, when this step is one.</para>
/// <para><c>PlayerAction</c> — The accepted action and its payment choices, when this step is one.</para>
/// <para><c>OccurrenceId</c> —
/// A dynamically allocated occurrence id, or null when round/number/index name it.
/// </para>
/// <para><c>SurgeGained</c> —
/// Whether this suspended reveal ability has already gained and resolved Surge.
/// This continuation flag is an engine representation choice; it preserves the
/// Rules Reference's one effective instance of a non-numeric keyword when the
/// ability resumes after a player choice.
/// </para>
/// <para><c>Discarded</c> —
/// Cards discarded earlier in a suspended ability, by object id. The spelling
/// is an engine representation choice; it preserves "discarded this way" bindings
/// when the ability resumes after a player choice.
/// </para>
/// <para><c>ActivatedEnemies</c> —
/// Enemies that have already activated in the current player's step-2
/// procedure, by object id. This is engine continuation data rather than a
/// Rules Reference term: it lets the procedure re-read the engaged-minion area
/// after every activation without activating an enemy twice.
/// </para>
/// <para><c>ActivationPlayers</c> —
/// The stable player order for the current step-2 procedure. This is engine
/// continuation data: eliminating a player changes <c>World.PlayerOrder</c>,
/// but must not make a continuation mistake the next surviving player for the
/// player whose enemies have already activated.
/// </para>
/// <para><c>ProcedureCandidates</c> —
/// The object ids offered by a rules procedure that suspended for a player
/// decision. The list is engine continuation data: the rulebook determines
/// who chooses and what is legal, while the agenda preserves the exact question
/// through nested scheduling and deterministic decision replay.
/// </para>
/// <para><c>ActivationOrder</c> —
/// The engaged player's chosen order for the remaining minion activations.
/// A minion that engages later is deliberately absent and starts a new ordering
/// question once this list has been exhausted.
/// </para>
/// <para><c>ProcedureAbilities</c> —
/// The stable addresses of simultaneous or optional abilities offered by a
/// suspended rules procedure.
/// </para>
/// <para><c>ProcedurePlayersPassed</c> —
/// Seats that passed the current optional procedure opportunity.
/// </para>
/// <para><c>ProcedureOccurrence</c> —
/// The rulebook occurrence local to a suspended procedure, when it differs
/// from the agenda occurrence that contains it.
/// </para>
/// <para><c>ProcedureOwnerOccurrence</c> —
/// The outer occurrence that owns a result produced by an internal procedure.
/// This is engine continuation data used when that result must join the
/// owner's eventual response window.
/// </para>
/// <para><c>ProcedureSource</c> — The source card needed when the procedure resumes.</para>
/// <para><c>ProcedureTrigger</c> — Event-stream provenance preserved by the procedure.</para>
/// <para><c>ProcedureVerb</c> — The kind of effect preserved by the procedure.</para>
/// <para><c>ProcedureBy</c> — The acting seat preserved by the procedure, or -1.</para>
/// <para><c>ProcedureAmount</c> — A numeric result preserved for procedure cleanup.</para>
/// <para><c>ProcedureAmounts</c> —
/// Numeric results shared by the frames of one rules procedure. The spelling
/// is engine continuation data; it lets a resumable procedure preserve a
/// per-card result across player windows without replaying the rule step.
/// </para>
/// <para><c>ProcedureFlag</c> — A boolean rule result preserved for procedure cleanup.</para>
/// <para><c>AbilityOrdinal</c> —
/// Which same-tier authored ability suspended. The ordinal is engine continuation data;
/// it avoids guessing when one card has more than one ability at the same timing.
/// </para>
/// <para><c>AbilityPath</c> —
/// Structural path from that ability's root effect to the node that suspended.
/// The segment spelling is owned by the typed continuation codec.
/// </para>
/// <para><c>AbilityActivationIds</c> — Activations one persisted ability is waiting for.</para>
/// <para><c>AbilityResults</c> — Effect-local numeric bindings carried across that wait.</para>
/// <para><c>AbilityOccurrence</c> — The occurrence the suspended ability is resolving in.</para>
/// <para><c>AbilityFace</c> — The printed face whose authored ability suspended.</para>
/// <para><c>AbilityPlayer</c> — The player resolving the containing ability.</para>
/// <para><c>AbilityActor</c> — The performer attributed to the containing labeled ability.</para>
/// <para><c>AbilityHasContinuation</c> — Whether structural ancestor work remains.</para>
public readonly record struct PhaseStep
{
    /// <summary>Build one step while callers migrate to typed operation factories.</summary>
    public PhaseStep(
        string What, int Round, int Number, int Index = 0, int Subject = -1, int Seat = -1,
        bool Plan = false, int Character = -1, Timing.AbilityType? Tier = null,
        ThreatPlacement? Placement = null, int ActivationId = -1, bool FinalStep = false,
        bool FinalPlayer = false, bool EachPlayerFrame = false, string Trigger = "",
        CharacterAttack? CharacterAttack = null, CharacterThwart? CharacterThwart = null,
        PlayerAction? PlayerAction = null, int? OccurrenceId = null,
        bool SurgeGained = false, IReadOnlyList<int>? Discarded = null,
        IReadOnlyList<int>? ActivatedEnemies = null,
        IReadOnlyList<int>? ActivationPlayers = null, int AbilityOrdinal = -1,
        IReadOnlyList<int>? ProcedureCandidates = null,
        IReadOnlyList<int>? ActivationOrder = null,
        IReadOnlyList<PendingAbility>? ProcedureAbilities = null,
        IReadOnlyList<int>? ProcedurePlayersPassed = null,
        Occurrence? ProcedureOccurrence = null, int ProcedureSource = -1,
        Occurrence? ProcedureOwnerOccurrence = null,
        string ProcedureTrigger = "", string ProcedureVerb = "", int ProcedureBy = -1,
        long ProcedureAmount = 0,
        IReadOnlyDictionary<int, long>? ProcedureAmounts = null,
        bool ProcedureFlag = false,
        IReadOnlyList<string>? AbilityPath = null,
        IReadOnlyList<int>? AbilityActivationIds = null,
        IReadOnlyDictionary<string, long>? AbilityResults = null,
        Occurrence? AbilityOccurrence = null,
        string AbilityFace = "", int AbilityPlayer = -1, int AbilityActor = -1,
        bool AbilityHasContinuation = false)
    {
        this.Round = Round;
        this.Number = Number;
        this.Index = Index;
        this.OccurrenceId = OccurrenceId;
        Operation = AgendaOperations.Create(What, new AgendaOperationData(
            Subject, Seat, Plan, Character, Tier, Placement, ActivationId, FinalStep,
            FinalPlayer, EachPlayerFrame, Trigger, CharacterAttack, CharacterThwart,
            PlayerAction, SurgeGained, Discarded, ActivatedEnemies, ActivationPlayers,
            AbilityOrdinal, ProcedureCandidates, ActivationOrder, ProcedureAbilities,
            ProcedurePlayersPassed, ProcedureOccurrence, ProcedureSource,
            ProcedureOwnerOccurrence, ProcedureTrigger, ProcedureVerb, ProcedureBy,
            ProcedureAmount, ProcedureAmounts, ProcedureFlag, AbilityPath,
            AbilityActivationIds, AbilityResults, AbilityOccurrence, AbilityFace,
            AbilityPlayer, AbilityActor, AbilityHasContinuation));
    }

    /// <summary>The closed operation payload.</summary>
    public AgendaOperation Operation { get; private init; }
    /// <summary>The round containing the step.</summary>
    public int Round { get; init; }
    /// <summary>The rule-defined number within its phase or procedure.</summary>
    public int Number { get; init; }
    /// <summary>The stable repetition index.</summary>
    public int Index { get; init; }
    /// <summary>An explicitly allocated occurrence identity, or null.</summary>
    public int? OccurrenceId { get; init; }

    /// <summary>The operation name used by timing conditions and event provenance.</summary>
    public string What
    {
        get => Operation.What;
        init => Operation = AgendaOperations.Change(Operation, value);
    }

    /// <summary>The primary object acted on, or -1.</summary>
    public int Subject { get => Operation.Data.Subject; init => Operation = Operation.Changed(Operation.Data with { Subject = value }); }
    /// <summary>The concerned player, or -1.</summary>
    public int Seat { get => Operation.Data.Seat; init => Operation = Operation.Changed(Operation.Data with { Seat = value }); }
    /// <summary>Whether the operation only schedules child work.</summary>
    public bool Plan { get => Operation.Data.Plan; init => Operation = Operation.Changed(Operation.Data with { Plan = value }); }
    /// <summary>A secondary character object, or -1.</summary>
    public int Character { get => Operation.Data.Character; init => Operation = Operation.Changed(Operation.Data with { Character = value }); }
    /// <summary>The ability type of a suspended authored ability.</summary>
    public Timing.AbilityType? Tier { get => Operation.Data.Tier; init => Operation = Operation.Changed(Operation.Data with { Tier = value }); }
    /// <summary>A frozen threat placement.</summary>
    public ThreatPlacement? Placement { get => Operation.Data.Placement; init => Operation = Operation.Changed(Operation.Data with { Placement = value }); }
    /// <summary>The owning enemy activation identity, or -1.</summary>
    public int ActivationId { get => Operation.Data.ActivationId; init => Operation = Operation.Changed(Operation.Data with { ActivationId = value }); }
    /// <summary>Whether this is the final effect in its authored sequence.</summary>
    public bool FinalStep { get => Operation.Data.FinalStep; init => Operation = Operation.Changed(Operation.Data with { FinalStep = value }); }
    /// <summary>Whether this is the final player frame.</summary>
    public bool FinalPlayer { get => Operation.Data.FinalPlayer; init => Operation = Operation.Changed(Operation.Data with { FinalPlayer = value }); }
    /// <summary>Whether this continuation belongs to an each-player frame.</summary>
    public bool EachPlayerFrame { get => Operation.Data.EachPlayerFrame; init => Operation = Operation.Changed(Operation.Data with { EachPlayerFrame = value }); }
    /// <summary>Event provenance retained across suspension.</summary>
    public string Trigger { get => Operation.Data.Trigger; init => Operation = Operation.Changed(Operation.Data with { Trigger = value }); }
    /// <summary>A queued player attack.</summary>
    public CharacterAttack? CharacterAttack { get => Operation.Data.CharacterAttack; init => Operation = Operation.Changed(Operation.Data with { CharacterAttack = value }); }
    /// <summary>A queued player thwart.</summary>
    public CharacterThwart? CharacterThwart { get => Operation.Data.CharacterThwart; init => Operation = Operation.Changed(Operation.Data with { CharacterThwart = value }); }
    /// <summary>An accepted player Action.</summary>
    public PlayerAction? PlayerAction { get => Operation.Data.PlayerAction; init => Operation = Operation.Changed(Operation.Data with { PlayerAction = value }); }
    /// <summary>Whether the containing reveal gained Surge.</summary>
    public bool SurgeGained { get => Operation.Data.SurgeGained; init => Operation = Operation.Changed(Operation.Data with { SurgeGained = value }); }
    /// <summary>Object ids discarded earlier in the containing ability.</summary>
    public IReadOnlyList<int>? Discarded { get => Operation.Data.Discarded; init => Operation = Operation.Changed(Operation.Data with { Discarded = value }); }
    /// <summary>Enemies already activated by the current activation procedure.</summary>
    public IReadOnlyList<int>? ActivatedEnemies { get => Operation.Data.ActivatedEnemies; init => Operation = Operation.Changed(Operation.Data with { ActivatedEnemies = value }); }
    /// <summary>The stable player order of the current activation procedure.</summary>
    public IReadOnlyList<int>? ActivationPlayers { get => Operation.Data.ActivationPlayers; init => Operation = Operation.Changed(Operation.Data with { ActivationPlayers = value }); }
    /// <summary>The same-type ordinal of a suspended authored ability.</summary>
    public int AbilityOrdinal { get => Operation.Data.AbilityOrdinal; init => Operation = Operation.Changed(Operation.Data with { AbilityOrdinal = value }); }
    /// <summary>Stable candidates retained by a suspended rules procedure.</summary>
    public IReadOnlyList<int>? ProcedureCandidates { get => Operation.Data.ProcedureCandidates; init => Operation = Operation.Changed(Operation.Data with { ProcedureCandidates = value }); }
    /// <summary>A chosen minion activation order.</summary>
    public IReadOnlyList<int>? ActivationOrder { get => Operation.Data.ActivationOrder; init => Operation = Operation.Changed(Operation.Data with { ActivationOrder = value }); }
    /// <summary>Ability addresses retained by a suspended rules procedure.</summary>
    public IReadOnlyList<PendingAbility>? ProcedureAbilities { get => Operation.Data.ProcedureAbilities; init => Operation = Operation.Changed(Operation.Data with { ProcedureAbilities = value }); }
    /// <summary>Players who passed the current procedure opportunity.</summary>
    public IReadOnlyList<int>? ProcedurePlayersPassed { get => Operation.Data.ProcedurePlayersPassed; init => Operation = Operation.Changed(Operation.Data with { ProcedurePlayersPassed = value }); }
    /// <summary>The occurrence local to a suspended rules procedure.</summary>
    public Occurrence? ProcedureOccurrence { get => Operation.Data.ProcedureOccurrence; init => Operation = Operation.Changed(Operation.Data with { ProcedureOccurrence = value }); }
    /// <summary>The procedure source object, or -1.</summary>
    public int ProcedureSource { get => Operation.Data.ProcedureSource; init => Operation = Operation.Changed(Operation.Data with { ProcedureSource = value }); }
    /// <summary>The outer occurrence that receives a procedure result.</summary>
    public Occurrence? ProcedureOwnerOccurrence { get => Operation.Data.ProcedureOwnerOccurrence; init => Operation = Operation.Changed(Operation.Data with { ProcedureOwnerOccurrence = value }); }
    /// <summary>Procedure event provenance.</summary>
    public string ProcedureTrigger { get => Operation.Data.ProcedureTrigger; init => Operation = Operation.Changed(Operation.Data with { ProcedureTrigger = value }); }
    /// <summary>The procedure effect verb.</summary>
    public string ProcedureVerb { get => Operation.Data.ProcedureVerb; init => Operation = Operation.Changed(Operation.Data with { ProcedureVerb = value }); }
    /// <summary>The procedure's acting seat, or -1.</summary>
    public int ProcedureBy { get => Operation.Data.ProcedureBy; init => Operation = Operation.Changed(Operation.Data with { ProcedureBy = value }); }
    /// <summary>A numeric procedure result.</summary>
    public long ProcedureAmount { get => Operation.Data.ProcedureAmount; init => Operation = Operation.Changed(Operation.Data with { ProcedureAmount = value }); }
    /// <summary>Per-card numeric procedure results.</summary>
    public IReadOnlyDictionary<int, long>? ProcedureAmounts { get => Operation.Data.ProcedureAmounts; init => Operation = Operation.Changed(Operation.Data with { ProcedureAmounts = value }); }
    /// <summary>A boolean procedure result.</summary>
    public bool ProcedureFlag { get => Operation.Data.ProcedureFlag; init => Operation = Operation.Changed(Operation.Data with { ProcedureFlag = value }); }
    /// <summary>The typed continuation codec's deterministic structural address.</summary>
    public IReadOnlyList<string>? AbilityPath { get => Operation.Data.AbilityPath; init => Operation = Operation.Changed(Operation.Data with { AbilityPath = value }); }
    /// <summary>Enemy activations awaited by an ability continuation.</summary>
    public IReadOnlyList<int>? AbilityActivationIds { get => Operation.Data.AbilityActivationIds; init => Operation = Operation.Changed(Operation.Data with { AbilityActivationIds = value }); }
    /// <summary>Effect-local numeric results retained across suspension.</summary>
    public IReadOnlyDictionary<string, long>? AbilityResults { get => Operation.Data.AbilityResults; init => Operation = Operation.Changed(Operation.Data with { AbilityResults = value }); }
    /// <summary>The occurrence containing a suspended ability.</summary>
    public Occurrence? AbilityOccurrence { get => Operation.Data.AbilityOccurrence; init => Operation = Operation.Changed(Operation.Data with { AbilityOccurrence = value }); }
    /// <summary>The printed face whose authored ability suspended.</summary>
    public string AbilityFace { get => Operation.Data.AbilityFace; init => Operation = Operation.Changed(Operation.Data with { AbilityFace = value }); }
    /// <summary>The player resolving the containing ability.</summary>
    public int AbilityPlayer { get => Operation.Data.AbilityPlayer; init => Operation = Operation.Changed(Operation.Data with { AbilityPlayer = value }); }
    /// <summary>The performer attributed to the containing labeled ability.</summary>
    public int AbilityActor { get => Operation.Data.AbilityActor; init => Operation = Operation.Changed(Operation.Data with { AbilityActor = value }); }
    /// <summary>Whether structural ancestor work remains.</summary>
    public bool AbilityHasContinuation { get => Operation.Data.AbilityHasContinuation; init => Operation = Operation.Changed(Operation.Data with { AbilityHasContinuation = value }); }

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
        int id = OccurrenceId ?? Moment.Id(Round, Number, Index);

        return What switch
        {
            Steps.PlaceThreat => VillainPhaseThreat(id, world, facts),
            Steps.SchemeThreat => SchemeThreat(id, world, facts),
            Steps.PlaceThreatEffect when Placement is { } placement =>
                Occurrence.ForThreat(id, Conditions, world, facts, placement),
            Steps.PlaceThreatEffect => throw new RulesNotImplementedException(
                "a scheduled threat placement has no placement payload"),
            Steps.Attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                Subject,
                Character >= 0 ? Character : world.Seats[Seat].IdentityCard.ObjectId,
                Seat),
            Steps.CharacterAttacks when (CharacterAttack ?? world.CharacterAttack) is { } attack =>
                Occurrence.ForAttack(
                    id,
                    Conditions,
                    world,
                    facts,
                    attack.Attacker,
                    attack.Enemy,
                    attack.Player),
            Steps.CharacterThwarts when (CharacterThwart ?? world.CharacterThwart) is { } thwart =>
                Occurrence.ForThwart(
                    id,
                    Conditions,
                    world,
                    facts,
                    thwart.Thwarter,
                    thwart.Scheme,
                    thwart.Player),
            Steps.PrepareIndirectAttackDamage => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                ProcedureSource,
                Subject,
                Seat),
            Steps.DealAttackDamage when world.Attack is { Indirect: true } attack =>
                Occurrence.ForAttack(
                    id, [], world, facts, attack.Enemy, attack.Target, attack.Player),
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

    private Occurrence VillainPhaseThreat(int id, World world, ICardFacts facts)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme)
        {
            return new Occurrence(id, Conditions);
        }

        long amount = StateFields.Modified(
                world, scheme, "escalation_threat", facts, world.Players)
            + MainScheme.Acceleration(world, facts);
        if (amount <= 0)
        {
            return new Occurrence(id, Conditions, Subject: scheme.ObjectId);
        }

        return Occurrence.ForThreat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, scheme.ObjectId, amount, ThreatCause.VillainPhase,
                "villain phase, place threat"));
    }

    private Occurrence SchemeThreat(int id, World world, ICardFacts facts)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme
            || Subject < 0 || Subject >= world.Cards.Count)
        {
            return new Occurrence(id, []);
        }

        var enemy = world.Cards[Subject];
        long amount = StateFields.Modified(
            world, enemy, "scheme", facts, world.Players);
        if (amount <= 0)
        {
            return Occurrence.ForThreat(
                id,
                [Steps.SchemeEnds],
                world,
                facts,
                new ThreatPlacement(
                    scheme.ObjectId, enemy.ObjectId, 0, ThreatCause.EnemyScheme,
                    "scheme", Seat),
                subject: enemy.ObjectId);
        }

        return Occurrence.ForThreat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, enemy.ObjectId, amount, ThreatCause.EnemyScheme,
                "scheme", Seat),
            subject: enemy.ObjectId);
    }

    /// <summary>An occurrence that needs no live attack roles, or null.</summary>
    public Occurrence? ScheduledOccurrence => What is
        Steps.Attack or Steps.CharacterAttacks or Steps.CharacterThwarts or Steps.EndAttack
            or Steps.PlaceThreat or Steps.SchemeThreat or Steps.PlaceThreatEffect
            or Steps.PrepareIndirectAttackDamage
            ? null
            : new Occurrence(
                OccurrenceId ?? Moment.Id(Round, Number, Index), Conditions, Subject, Seat);
}
