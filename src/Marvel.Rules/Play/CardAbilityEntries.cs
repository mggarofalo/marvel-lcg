using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Admits card entry batches before their implementation can schedule or execute work.</summary>
public static class CardAbilityEntries
{
    /// <summary>Execute a card's setup abilities inside one owned batch.</summary>
    public static AbilityExecutionResult ExecuteSetup(this ICardSetupAbilities abilities, World world, Card card) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [Steps.Setup], Subject: card.ObjectId, Player: card.Owner),
            entry => abilities.Setup(world, entry, card));

    /// <summary>Execute entry text before its owning placement can complete.</summary>
    public static AbilityExecutionResult ExecuteEntry(this IEncounterCardAbilities abilities, World world, Card card) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [Steps.CardEntersPlay], Subject: card.ObjectId, Player: card.Owner),
            entry => abilities.EntersPlay(world, entry, card));

    /// <summary>Execute all reveal text within the reveal's retained triggering occurrence.</summary>
    public static AbilityExecutionResult ExecuteReveal(this IEncounterCardAbilities abilities,
        World world, Card card, int player, Occurrence? occurrence = null) =>
        world.Agenda.Executions.RunBatch(
            occurrence ?? new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player),
            entry => abilities.WhenRevealed(world, entry, card, player));

    /// <summary>Execute a boost star before the activation proceeds to its next step.</summary>
    public static AbilityExecutionResult ExecuteBoost(this IAttackCardAbilities abilities,
        World world, Card card, int player) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player),
            entry => abilities.Boost(world, entry, card, player));

    /// <summary>Execute completion-triggered text before releasing the activation's waiting abilities.</summary>
    public static AbilityExecutionResult CompleteActivation(this IActivationCompletionAbilities abilities,
        World world, EnemyActivation result) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, ["WhenActivationCompleted"], Actor: result.Enemy, Player: result.Player),
            entry => abilities.ActivationCompleted(world, entry, result));

    /// <summary>Execute one explicitly instructed Special ability.</summary>
    public static AbilityExecutionResult ExecuteSpecial(this ICardContinuationAbilities abilities,
        World world, Card card, int player, bool finalStep) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [Steps.ResolveSpecial], Subject: card.ObjectId, Player: player),
            entry => abilities.ResolveSpecial(world, entry, card, player, finalStep));
    /// <summary>Resolve step-seven text while the exact defeated card remains in play.</summary>
    public static AbilityExecutionResult ExecuteDefeat(this ICardDamageAbilities abilities,
        World world, Card card, Defeated defeated, string trigger)
    {
        var occurrence = new Occurrence(0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);
        occurrence.Also(defeated);
        return world.Agenda.Executions.RunBatch(occurrence,
            entry => abilities.WhenCardDefeated(world, entry, card, defeated, trigger));
    }

    /// <summary>Open one ordering window for simultaneous defeated cards.</summary>
    internal static AbilityExecutionResult ExecuteDefeats(this ICardDamageAbilities abilities,
        World world, IReadOnlyList<Defeated> defeated, string trigger)
    {
        var occurrence = new Occurrence(0, [Steps.CardDefeated], Subject: defeated[0].Card,
            Player: world.FirstPlayer);
        foreach (var item in defeated) occurrence.Also(item);
        return world.Agenda.Executions.RunBatch(occurrence,
            entry => abilities.WhenCardsDefeated(world, entry, defeated, trigger));
    }

    /// <summary>Resolve all interrupts to one imminent defeat before deciding whether it still happens.</summary>
    public static AbilityExecutionResult ExecuteWouldDefeat(this ICardDamageAbilities abilities,
        World world, Card target, DefeatCause cause) =>
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner),
            entry => abilities.WouldBeDefeated(world, entry, target, cause));

}
