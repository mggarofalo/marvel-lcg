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
    public static bool Handles(PhaseStep step) => step.What is
        Steps.CompleteAttackActivation or Steps.CompleteSchemeActivation
        or Steps.Attack or Steps.GiveBoostCard or Steps.DeclareDefender
        or Steps.FlipBoostCards or Steps.FinishBoostCard
        or Steps.CalculateAttackDamage or Steps.DealAttackDamage
        or Steps.AssignIndirectAttackDamage or Steps.PrepareIndirectAttackDamage
        or Steps.ApplyIndirectAttackDamage or Steps.FinishIndirectAttackDamage
        or Steps.NextAttackTarget or Steps.CharacterAttacks or Steps.CharacterThwarts
        or Steps.AllyConsequentialDamage or Steps.AllyThwartConsequentialDamage
        or Steps.EndAttack or Steps.FinishAttackDamage;

    public static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.CompleteAttackActivation:
            case Steps.CompleteSchemeActivation:
                CompleteActivation(world, world.ActivationCompletionAbilities, step, events);
                break;
            case Steps.Attack:
                Attack.Initiate(world, facts, step, events);
                break;
            case Steps.GiveBoostCard:
                Attack.GiveBoostCard(world, facts, events);
                break;
            case Steps.DeclareDefender:
                return Attack.DeclareDefender(world, facts, world.AttackAbilities);
            case Steps.FlipBoostCards:
                Attack.FlipBoostCards(world, facts, world.AttackAbilities, events);
                break;
            case Steps.FinishBoostCard:
                Attack.FinishBoostCard(world, facts, world.AttackAbilities, step, events);
                break;
            case Steps.CalculateAttackDamage:
                Attack.CalculateDamage(world, facts);
                break;
            case Steps.DealAttackDamage:
                Attack.DealDamage(world, facts, events);
                break;
            case Steps.AssignIndirectAttackDamage:
                return Attack.IndirectDamagePrompt(world, facts, step);
            case Steps.PrepareIndirectAttackDamage:
                break;
            case Steps.ApplyIndirectAttackDamage:
                Attack.ApplyIndirectDamage(world, facts, step, events);
                break;
            case Steps.FinishIndirectAttackDamage:
                Attack.FinishIndirectDamage(world, facts, step, events);
                break;
            case Steps.NextAttackTarget:
                Attack.NextTarget(world, step.Seat);
                break;
            case Steps.CharacterAttacks:
                BasicPowers.ResolveCharacterAttack(world, facts, events, step.CharacterAttack);
                break;
            case Steps.CharacterThwarts:
                BasicPowers.ResolveCharacterThwart(world, facts, events, step.CharacterThwart);
                break;
            case Steps.AllyConsequentialDamage:
            case Steps.AllyThwartConsequentialDamage:
                AllyConsequentialDamage(world, facts, step, events);
                break;
            case Steps.EndAttack:
                Attack.End(world, events);
                break;
            case Steps.FinishAttackDamage:
                Damage.FinishAttack(world, facts, step, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the attack procedure has no step '{step.What}'");
        }

        return null;
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

        BasicPowers.Consequential(
            world,
            facts,
            world.Cards[step.Subject],
            byAttack: attacked || BasicPowers.Assaulted(world, facts, world.Cards[step.Character]),
            attacked ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb,
            events);
    }
}
