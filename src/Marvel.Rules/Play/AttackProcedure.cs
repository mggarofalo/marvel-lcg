using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

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
            case Steps.FinishAttackDamage:
                DamageAttacks.FinishAttack(world, facts, step, events);
                return true;
            default: return false;
        }
    }

    private static bool TryApplyCharacterPower(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.CharacterAttacks:
                BasicPowerResolution.ResolveCharacterAttack(world, facts, events, step.CharacterAttack);
                return true;
            case Steps.CharacterThwarts:
                BasicPowerResolution.ResolveCharacterThwart(world, facts, events, step.CharacterThwart);
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
            : new EnemyActivation(
                step.Subject, step.Seat, attacking, step.ActivationId, Made: false);

        world.FinishedActivation = result;
        events.AddRange(abilities.ActivationCompleted(world, result));
        world.FinishedActivation = null;
        world.Activation = null;
    }

    private static void AllyConsequentialDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        bool attacked = string.Equals(
            step.What, Steps.AllyConsequentialDamage, StringComparison.Ordinal);

        BasicPowerResolution.Consequential(
            world,
            facts,
            world.Cards[step.Subject],
            byAttack: attacked || BasicPowerStatus.Assaulted(world, facts, world.Cards[step.Character]),
            attacked ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb,
            events);
    }
}
