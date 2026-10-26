namespace Marvel.Cards.Run;

// Engine choice: only the exact occurrence's captured attack placements supply this number.
// Current health, boost damage and a later activation cannot supply or overwrite it.
internal static class ReceivedAttackDamageQuery
{
    internal static long Amount(AbilityExpressionContext context)
    {
        int player = AbilityCardQueries.Resolver(context.Bindings);
        var identity = context.World.Seats[player].IdentityCard;
        return context.Occurrence.ReceivedAttackDamage.Where(damage =>
            damage.Attacker.Card == context.Occurrence.Actor
            && damage.Attacker.Incarnation == context.Occurrence.ActorFacts?.Incarnation
            && damage.Recipient.Card == identity.ObjectId
            && damage.Recipient.Incarnation == identity.Incarnation
            && damage.Recipient.Controller == player
            && (context.Bindings.TriggeringAttack is not { } triggering
                || triggering.Card == damage.Recipient.Card
                    && triggering.Incarnation == damage.Recipient.Incarnation)).Sum(damage => damage.Amount);
    }
}
