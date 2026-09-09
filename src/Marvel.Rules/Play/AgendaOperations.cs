using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The phase-neutral procedure that owns an agenda operation.</summary>
public enum AgendaProcedureKind
{
    /// <summary>Enemy and player attack procedures.</summary>
    Attack,
    /// <summary>Threat placement and enemy scheme procedures.</summary>
    Threat,
    /// <summary>Encounter-card reveal procedures.</summary>
    Reveal,
    /// <summary>Character and scheme defeat procedures.</summary>
    Defeat,
    /// <summary>Accepted player actions and limit choices.</summary>
    PlayerAction,
    /// <summary>Card-ability continuation procedures.</summary>
    AbilityContinuation,
    /// <summary>Villain-phase enemy activation planning.</summary>
    Activation,
    /// <summary>Player and villain phase transitions.</summary>
    PhaseTransition,
    /// <summary>Lifecycle occurrences whose mutation already happened.</summary>
    Lifecycle,
}

/// <summary>A closed operation payload stored by one agenda step.</summary>
/// <remarks>
/// The operation vocabulary and its grouping are engine choices. Rules clauses
/// determine what each operation does and when it occurs; they do not prescribe
/// a software payload format.
/// </remarks>
public abstract record AgendaOperation
{
    private protected AgendaOperation(
        string what, AgendaProcedureKind procedure, AgendaOperationData data)
    {
        What = what;
        Procedure = procedure;
        Data = data;
    }

    /// <summary>The operation name used by timing conditions.</summary>
    public string What { get; }

    /// <summary>The procedure owner category.</summary>
    public AgendaProcedureKind Procedure { get; }

    internal AgendaOperationData Data { get; }

    private protected abstract AgendaOperation WithData(AgendaOperationData data);

    internal AgendaOperation Changed(AgendaOperationData data) => WithData(data);
}

internal sealed record AttackAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Attack, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new AttackAgendaOperation(Name, data);
}

internal sealed record ThreatAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Threat, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new ThreatAgendaOperation(Name, data);
}

internal sealed record RevealAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Reveal, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new RevealAgendaOperation(Name, data);
}

internal sealed record DefeatAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Defeat, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new DefeatAgendaOperation(Name, data);
}

internal sealed record PlayerActionAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.PlayerAction, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new PlayerActionAgendaOperation(Name, data);
}

internal sealed record AbilityContinuationAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.AbilityContinuation, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new AbilityContinuationAgendaOperation(Name, data);
}

internal sealed record ActivationAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Activation, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new ActivationAgendaOperation(Name, data);
}

internal sealed record PhaseTransitionAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.PhaseTransition, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new PhaseTransitionAgendaOperation(Name, data);
}

internal sealed record LifecycleAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Lifecycle, Payload)
{
    private protected override AgendaOperation WithData(AgendaOperationData data) =>
        new LifecycleAgendaOperation(Name, data);
}

internal sealed record AgendaOperationData(
    int Subject = -1,
    int Seat = -1,
    bool Plan = false,
    int Character = -1,
    AbilityType? Tier = null,
    ThreatPlacement? Placement = null,
    int ActivationId = -1,
    bool FinalStep = false,
    bool FinalPlayer = false,
    bool EachPlayerFrame = false,
    string Trigger = "",
    CharacterAttack? CharacterAttack = null,
    CharacterThwart? CharacterThwart = null,
    PlayerAction? PlayerAction = null,
    bool SurgeGained = false,
    IReadOnlyList<int>? Discarded = null,
    IReadOnlyList<int>? ActivatedEnemies = null,
    IReadOnlyList<int>? ActivationPlayers = null,
    int AbilityOrdinal = -1,
    IReadOnlyList<int>? ProcedureCandidates = null,
    IReadOnlyList<int>? ActivationOrder = null,
    IReadOnlyList<PendingAbility>? ProcedureAbilities = null,
    IReadOnlyList<int>? ProcedurePlayersPassed = null,
    Occurrence? ProcedureOccurrence = null,
    int ProcedureSource = -1,
    Occurrence? ProcedureOwnerOccurrence = null,
    string ProcedureTrigger = "",
    string ProcedureVerb = "",
    int ProcedureBy = -1,
    long ProcedureAmount = 0,
    IReadOnlyDictionary<int, long>? ProcedureAmounts = null,
    bool ProcedureFlag = false,
    IReadOnlyList<string>? AbilityPath = null,
    IReadOnlyList<int>? AbilityActivationIds = null,
    IReadOnlyDictionary<string, long>? AbilityResults = null,
    Occurrence? AbilityOccurrence = null,
    string AbilityFace = "",
    int AbilityPlayer = -1,
    int AbilityActor = -1,
    bool AbilityHasContinuation = false);

internal static class AgendaOperations
{
    public static AgendaOperation Create(string what, AgendaOperationData data) => what switch
    {
        Steps.CompleteAttackActivation or Steps.CompleteSchemeActivation
            or Steps.Attack or Steps.GiveBoostCard or Steps.DeclareDefender
            or Steps.FlipBoostCards or Steps.FinishBoostCard
            or Steps.CalculateAttackDamage or Steps.DealAttackDamage
            or Steps.AssignIndirectAttackDamage or Steps.PrepareIndirectAttackDamage
            or Steps.ApplyIndirectAttackDamage or Steps.FinishIndirectAttackDamage
            or Steps.NextAttackTarget or Steps.CharacterAttacks or Steps.CharacterThwarts
            or Steps.AllyConsequentialDamage or Steps.AllyThwartConsequentialDamage
            or Steps.EndAttack or Steps.FinishAttackDamage =>
                new AttackAgendaOperation(what, AttackPayload(data)),

        Steps.PlaceThreat or Steps.PlaceThreatEffect or Steps.Scheme
            or Steps.SchemeThreat or Steps.EndSchemeEarly =>
                new ThreatAgendaOperation(what, data),

        Steps.DealEncounterCards or Steps.RevealEncounterCards
            or Steps.RevealEncounterCard or Steps.DiscardRevealedTreachery
            or Steps.ChooseAttachmentTarget or Steps.ChooseRevealAbility
            or Steps.ResumeRevealAbility or Steps.ChoosePostRevealAbility
            or Steps.FinalizeAllyEntry => new RevealAgendaOperation(what, data),

        Steps.FinalizeCharacterDefeat or Steps.FinalizeSchemeDefeat
            or Steps.ChooseWouldBeDefeated or Steps.ResumeWouldBeDefeated
            or Steps.ChooseCardDefeatedAbility or Steps.ResumeCardDefeatedAbility =>
                new DefeatAgendaOperation(what, data),

        Steps.TurnAction or Steps.ChooseAllyForLimit or Steps.ChooseRestrictedCard =>
            new PlayerActionAgendaOperation(what, data),

        Steps.ResumeAbility or Steps.ResolveSpecial or Steps.ChooseOption
            or Steps.OrderEachPlayer or Steps.ResolveEachPlayer =>
                new AbilityContinuationAgendaOperation(what, data),

        Steps.EnemiesActivate => new ActivationAgendaOperation(what, data),

        Steps.PassFirstPlayerToken or Steps.EndVillainPhase or Steps.DrawToHandSize
            or Steps.ReadyCards or Steps.EndPlayerPhase =>
                new PhaseTransitionAgendaOperation(what, data),

        Steps.CardPlayed or Steps.EventPlayed or Steps.CardEntersPlay or Steps.FormChanged =>
            new LifecycleAgendaOperation(what, data),

        _ => throw new RulesNotImplementedException(
            $"the agenda has no operation payload for '{what}'"),
    };

    public static AgendaOperation Change(AgendaOperation operation, string what)
    {
        AgendaOperation changed = Create(what, operation.Data);
        if (changed.Procedure != operation.Procedure)
        {
            throw new InvalidOperationException(
                $"an {operation.Procedure} operation cannot become a "
                + $"{changed.Procedure} operation");
        }
        return changed;
    }

    private static AgendaOperationData AttackPayload(AgendaOperationData data)
    {
        if (data.Placement is not null
            || data.PlayerAction is not null
            || data.ProcedureAbilities is not null
            || data.ProcedurePlayersPassed is not null
            || data.AbilityOrdinal >= 0
            || data.AbilityPath is not null
            || data.AbilityActivationIds is not null
            || data.AbilityResults is not null
            || data.AbilityOccurrence is not null
            || data.AbilityFace.Length > 0
            || data.AbilityPlayer >= 0
            || data.AbilityActor >= 0
            || data.AbilityHasContinuation)
        {
            throw new ArgumentException(
                "an attack operation cannot carry reveal, action, or ability-continuation payload");
        }
        return data;
    }
}
