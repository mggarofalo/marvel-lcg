using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

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

    internal static AgendaOperationData AttackPayload(AgendaOperationData data)
    {
        if (HasRevealOrActionPayload(data) || HasAbilityPayload(data))
        {
            throw new ArgumentException(
                "an attack operation cannot carry reveal, action, or ability-continuation payload");
        }
        return data;
    }

    private static bool HasRevealOrActionPayload(AgendaOperationData data) =>
        data.Placement is not null || data.PlayerAction is not null
        || data.ProcedureAbilities is not null || data.ProcedurePlayersPassed is not null;

    private static bool HasAbilityPayload(AgendaOperationData data) =>
        data.AbilityOrdinal >= 0 || data.AbilityPath is not null
        || data.AbilityActivationIds is not null || data.AbilityResults is not null
        || data.AbilityOccurrence is not null || data.AbilityFace.Length > 0
        || data.AbilityPlayer >= 0 || data.AbilityActor >= 0 || data.AbilityHasContinuation;
}
