using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Resolves and orders abilities answering a completed card defeat.</summary>
internal static class DefeatedCardAbilityResolution
{
    internal static IReadOnlyList<GameEvent> WhenCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, Defeated defeated, string trigger, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(defeated);

        return execution.WhenCardsDefeated(world, [defeated], trigger, occurrence);
    }

    internal static IReadOnlyList<GameEvent> WhenCardsDefeated(this AbilityResolutionExecution execution,
        World world, IReadOnlyList<Defeated> defeats, string trigger, Occurrence occurrence)
    {
        var own = defeats.SelectMany(defeat => Own(execution, world, world.Cards[defeat.Card], defeat)).ToArray();
        var defeated = defeats[0];
        var card = world.Cards[defeated.Card];
        if (own.Length == 0 && execution.Answering(world, occurrence, world.Agenda.Occurrence).Count == 0)
            return [];

        var resume = new PhaseStep(
            new DefeatAbilityOperation(DefeatAbilityKind.ResumeCardDefeatedAbility,
                new DefeatCause(Trigger: trigger, Verb: defeated.How, By: defeated.By),
                new DefeatWindow(occurrence) { OwnAbilities = own }),
            world.Agenda.Current?.Round ?? 0, 7, Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer, Plan: true);
        if (world.Agenda.Current is { } owner && world.Agenda.Occurrence is { } parent)
            world.Agenda.ContinueBeforeOwner(parent, owner, resume);
        else world.Agenda.Add(resume);
        return [];
    }
    private static PendingAbility[] Own(AbilityResolutionExecution execution, World world,
        Card card, Defeated defeated)
    {
        if (PrintedText.IsBlanked(world, card)) return [];
        var written = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        // Authored definitions execute independently of the printed-text diagnostic.
        if (written.Count == 0 && world.Facts.HasWhenDefeated(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                + "ability that no ability data is written for");
        }

        return written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal)
            { TriggeringDefeat = defeated }).ToArray();
    }
}
