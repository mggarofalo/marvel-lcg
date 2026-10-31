using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Connects an explicitly simultaneous authored attack to its placement barrier.</summary>
internal static class AbilitySimultaneousAttackDamage
{
    internal static bool Resolve(AbilityEffect.AttackDamage damage,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state,
        Card attacker, long amount)
    {
        if (context.Power != BasicPowers.AttackVerb)
            throw new RulesNotImplementedException("simultaneous attack damage requires a labelled attack");
        var occurrence = context.PowerOccurrence
            ?? throw new RulesNotImplementedException("simultaneous attack damage requires a labelled power occurrence");
        if (occurrence.Attacks.Count != 0)
            throw new RulesNotImplementedException("multiple simultaneous damage nodes in one labelled attack");
        var entries = AbilityDamageAndThreatQueries.Every(damage.Cards, context);
        var targets = AbilityAttackRecipients.Admit(context, attacker, [.. entries.DistinctBy(card => card.ObjectId)]);
        AbilityAttackRecipients.RequireNoSimultaneousLateAnswer(context, targets);
        foreach (var target in targets)
            if (occurrence.Retaliation is { } retaliation)
                occurrence.Retaliation = retaliation.Include(target, context.World.Facts);
        var recipients = context.DeclaredAttackRecipients.IsDefault
            ? targets.Select(card => Attacked.Capture(card, context.World.Facts)).ToArray()
            : [.. context.DeclaredAttackRecipients.DistinctBy(recipient => (recipient.Card, recipient.Incarnation))];
        if (recipients.Length == 0) return false;
        var result = targets.Count == 0 ? null : SimultaneousAttackDamage.Resolve(context.World, context.World.Facts,
            new(attacker, context.Source, targets, amount, context.Trigger, "Attack")
            { Retaliate = false, Amounts = damage.Distributed ? Shares(entries, targets, amount) : null },
            context.Events);
        state.Attacked.AddRange(targets);
        (state.Damage ??= new(context.Occurrence)).Add(
            new AbilitySimultaneousDamageCompletion(result, recipients,
                [.. targets.Select(card => card.ObjectId)], amount, occurrence));
        return result is { IsComplete: false };
    }

    // Each allocated point is one unit of the authored 1 damage; instance modifiers
    // already included in the per-recipient amount apply once to each recipient.
    private static long[] Shares(IReadOnlyList<Card> entries, IReadOnlyList<Card> targets, long amount) =>
        [.. targets.Select(target => amount - 1 + entries.Count(entry => entry.ObjectId == target.ObjectId))];
}
