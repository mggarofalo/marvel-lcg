using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Separates the responsible player from identity-only credit for damage and threat removal.</summary>
internal static class AbilityEffectAttribution
{
    internal static int ResponsiblePlayer(AbilityDamageAndThreatContext context) =>
        AbilityCardQueries.IsPlayerCard(context.World.Facts, context.Source)
            ? ResponsiblePlayer(context.World, context.Source, AbilityDamageAndThreatQueries.Resolver(context)) : -1;

    internal static int ResponsiblePlayer(World world, Card source, int player) =>
        // rr:ownership-and-control.2: player cards enter play under their owner's control.
        // The captured ability resolver retains the acting controller across nested effects;
        // encounter-card effects do not become player actions merely by asking a player a question.
        player >= 0 && player < world.Players && AbilityCardQueries.IsPlayerCard(world.Facts, source)
            ? player : -1;

    internal static int IdentityPlayer(AbilityDamageAndThreatContext context) =>
        AbilityCardQueries.IsPlayerCard(context.World.Facts, context.Source)
            ? IdentityPlayer(context.World, context.Source,
                AbilityDamageAndThreatQueries.Resolver(context), context.PowerActor ?? context.AbilityActor) : -1;

    internal static int IdentityPlayer(World world, Card source, int player, Card? actor = null)
    {
        if (player < 0 || player >= world.Players) return -1;
        var performer = actor ?? LabeledAbilities.Performer(world, world.Facts, player, source);
        // rr:you-your.12/.14: events and eligible upgrades extend the identity;
        // rr:you-your.15/.18: allies and supports do not.
        return LabeledAbilities.IdentityPlayer(world, performer);
    }
}
