using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Coordinates defeat interrupt windows and the resulting card departure.</summary>

internal static class DefeatProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.FinalizeCharacterDefeat:
                FinalizeCharacter(world, facts, step, events);
                break;
            case Steps.FinalizeSchemeDefeat:
                FinalizeScheme(world, facts, step, events);
                break;
            case Steps.ChooseWouldBeDefeated:
                return ChooseWouldBeDefeated(world, world.WindowAbilities, step);
            case Steps.ResumeWouldBeDefeated:
                ResumeWouldBeDefeated(world, facts, world.WindowAbilities, step, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                return ChooseCardDefeatedAbility(world, world.WindowAbilities, step);
            case Steps.ResumeCardDefeatedAbility:
                DefeatWindowResumption.Resume(world, facts, world.WindowAbilities, step, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the defeat procedure has no step '{step.What}'");
        }
        return null;
    }

    internal static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseWouldBeDefeated:
                DefeatChoiceResolution.Resolve(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                DefeatChoiceResolution.Resolve(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"defeat step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    internal static void ScheduleProcedureChoice(World world, PhaseStep step)
    {
        if (world.Agenda.Occurrence is { } occurrence)
        {
            world.Agenda.ThenContinuation(step, occurrence);
            return;
        }

        world.Agenda.Add(step with { Plan = true });
    }

    internal static Prompt ChooseWouldBeDefeated(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.Require<DefeatAbilityOperation>().Window.Abilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 6 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = DefeatChoiceResolution.Player(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardWouldBeDefeated,
            mandatory
                ? $"order abilities before card {step.Subject} is defeated"
                : $"interrupt before card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void ResumeWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        if (DamagePlacement.Health(world, facts, target) - target.Damage > 0)
        {
            world.Effects.CompleteHealthDefeat(target);
            return;
        }

        DefeatWindowResumption.Resume(world, facts, abilities, step, events);
    }

    internal static void FinishWouldBeDefeated(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        world.Effects.CompleteHealthDefeat(target);
        if (DeckTypes.IsInPlay(target.Area.Type)
            && DamagePlacement.Health(world, facts, target) - target.Damage <= 0)
        {
            Defeat.Character(
                world, facts, target, step.Require<DefeatAbilityOperation>().Cause.Trigger, events,
                how: step.Require<DefeatAbilityOperation>().Cause.Verb, by: step.Require<DefeatAbilityOperation>().Cause.By,
                recordOn: step.Require<DefeatAbilityOperation>().Cause.Owner);
        }
    }

    internal static Prompt ChooseCardDefeatedAbility(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.Require<DefeatAbilityOperation>().Window.Abilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 7 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = DefeatChoiceResolution.Player(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardDefeated,
            mandatory
                ? $"order abilities when card {step.Subject} is defeated"
                : $"interrupt when card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void FinalizeCardDefeat(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        if (EffectiveCards.Kind(card, facts) == CardKind.EncounterSideScheme)
        {
            Defeat.FinalizeScheme(world, facts, card, step.Require<DefeatAbilityOperation>().Cause.Trigger, events);
        }
        else
        {
            Defeat.FinalizeCharacter(world, facts, card, step.Require<DefeatAbilityOperation>().Cause.Trigger, events);
        }
    }

    internal static void FinalizeCharacter(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeCharacter(
            world, facts, world.Cards[step.Subject], step.Require<DefeatCompletionOperation>().Trigger, events);

    internal static void FinalizeScheme(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeScheme(
            world, facts, world.Cards[step.Subject], step.Require<DefeatCompletionOperation>().Trigger, events);
}
