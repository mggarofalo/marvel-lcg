using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Applies and answers attack-family agenda operations.</summary>
/// <remarks>
/// This procedure boundary is an engine choice. It keeps attack execution out
/// of the phase planner while <see cref="Sequence"/> retains timing progression.
/// </remarks>
internal static class AttackProcedure
{
    public static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        if (step.Operation is BoostAbilityOperation)
        {
            BoostAbilityResolution.ResolveAbility(world, facts, world.AttackAbilities, step, events);
            return null;
        }
        if (step.Operation is BasicAttackCostOperation) return BasicAttackCosts.Describe(world, step);
        if (step.Operation is NativeRetaliationOperation)
        {
            NativeRetaliationResolution.Resume(world, step, events);
            return null;
        }
        if (step.Operation is SimultaneousAttackDamageOperation)
        {
            SimultaneousAttackDamageResolution.Resume(world, step, events);
            return null;
        }
        if (step.Operation is AttackDamageResolutionOperation)
        {
            AttackDamageResolution.Resume(world, step, events);
            return null;
        }
        if (TryApplyActivation(world, facts, step, events, out Prompt? prompt)) return prompt;
        if (TryApplyDamage(world, facts, step, events, out prompt)) return prompt;
        if (TryApplyCharacterPower(world, facts, step, events)) return null;
        throw new RulesNotImplementedException(
            $"the attack procedure has no step '{step.What}'");
    }

    private static bool TryApplyActivation(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events,
        out Prompt? prompt)
    {
        prompt = null;
        switch (step.What)
        {
            case Steps.CompleteAttackActivation:
            case Steps.CompleteSchemeActivation:
                CompleteActivation(world, world.ActivationCompletionAbilities, step, events);
                return true;
            case Steps.Attack:
                Attack.Initiate(world, facts, step, events);
                return true;
            case Steps.GiveBoostCard:
                Attack.GiveBoostCard(world, facts, events);
                return true;
            case Steps.DeclareDefender:
                prompt = Attack.DeclareDefender(world, facts, world.AttackAbilities);
                return true;
            case Steps.FlipBoostCards:
                Attack.FlipBoostCards(world, facts, world.AttackAbilities, events);
                return true;
            case BoostFaceupOperation.Faceup:
                BoostAbilityResolution.ResolveFaceup(world, step);
                return true;
            case Steps.FinishBoostCard:
                Attack.FinishBoostCard(world, facts, world.AttackAbilities, step, events);
                return true;
            case Steps.CalculateAttackDamage:
                Attack.CalculateDamage(world, facts);
                return true;
            default: return false;
        }
    }

    private static bool TryApplyDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events,
        out Prompt? prompt)
    {
        prompt = null;
        switch (step.What)
        {
            case Steps.DealAttackDamage:
                Attack.DealDamage(world, facts, events);
                return true;
            case Steps.AssignIndirectAttackDamage:
                prompt = Attack.IndirectDamagePrompt(world, facts, step);
                return true;
            case Steps.PrepareIndirectAttackDamage:
                return true;
            case Steps.ApplyIndirectAttackDamage:
                Attack.ApplyIndirectDamage(world, facts, step, events);
                return true;
            case Steps.FinishIndirectAttackDamage:
                Attack.FinishIndirectDamage(world, facts, step, events);
                return true;
            case Steps.NextAttackTarget:
                Attack.NextTarget(world, step.Seat);
                return true;
            case Steps.EndAttack:
                Attack.End(world, events);
                return true;
            default: return false;
        }
    }

    private static bool TryApplyCharacterPower(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case LateAttackRecipientOperation.Step:
                // The occurrence is the announcement; the damage that follows
                // resumes in the owning attack-damage procedure.
                return true;
            case Steps.CharacterAttacks:
                BasicPowerResolution.ResolveCharacterAttack(world, facts, events, step.Require<CharacterAttackOperation>().Attack);
                return true;
            case Steps.CharacterThwarts:
                BasicPowerResolution.ResolveCharacterThwart(world, facts, events, step.Require<CharacterThwartOperation>().Thwart);
                return true;
            case Steps.AllyConsequentialDamage:
            case Steps.AllyThwartConsequentialDamage:
                AllyConsequentialDamage(world, facts, step, events);
                return true;
            default: return false;
        }
    }

    public static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        if (step.Operation is BasicAttackCostOperation)
        {
            BasicAttackCosts.Answer(world, step, input, events);
            return;
        }
        switch (step.What)
        {
            case Steps.DeclareDefender:
                Attack.Defend(world, facts, world.AttackAbilities, input, events);
                break;
            case Steps.AssignIndirectAttackDamage:
                Attack.AssignIndirectDamage(world, facts, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"attack step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    private static void CompleteActivation(
        World world, IActivationCompletionAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        bool attacking = step.What == Steps.CompleteAttackActivation;
        var result = world.FinishedActivation is { } finished
            && finished.Id == step.ActivationId
            ? finished
            : world.Activation is { } active && active.Id == step.ActivationId
                ? active : new EnemyActivation(
                step.Subject, step.Seat, attacking, step.ActivationId, Made: false);

        world.Effects.CompleteActivation(step.ActivationId, events);
        world.FinishedActivation = result;
        ActivityBatches.Append(events, abilities.CompleteActivation(world, result).Events);
        world.FinishedActivation = null;
        if (world.Activation?.Id == step.ActivationId) world.Activation = null;
    }

    private static void AllyConsequentialDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<ConsequentialDamageOperation>();
        var ally = world.Cards[step.Subject];
        // rr:leaves-play.1: a departing card "is considered to be a new copy".
        // A labelled power cannot damage a different incarnation of its performer.
        if (operation.PerformerIncarnation is { } incarnation
            && (ally.Incarnation != incarnation || !DeckTypes.IsInPlay(ally.Area.Type))) return;
        bool attacked = string.Equals(
            step.What, Steps.AllyConsequentialDamage, StringComparison.Ordinal);

        BasicPowerResolution.Consequential(
            world,
            facts,
            ally,
            byAttack: attacked || operation.UsesAttack
                || operation.Character >= 0 && BasicPowerStatus.Assaulted(world, facts, world.Cards[operation.Character]),
            attacked ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb,
            events);
    }
}
