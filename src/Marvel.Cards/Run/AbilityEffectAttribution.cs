using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Identifies damage performed by an identity or one of its card extensions.</summary>
internal static class AbilityEffectAttribution
{
    internal static int IdentityPlayer(AbilityDamageAndThreatContext context) =>
        IdentityPlayer(context.World, context.Source,
            AbilityDamageAndThreatQueries.Resolver(context), context.PowerActor ?? context.AbilityActor);

    internal static int IdentityPlayer(World world, Card source, int player, Card? actor = null)
    {
        if (player < 0 || player >= world.Players) return -1;
        var performer = actor ?? LabeledAbilities.Performer(world, world.Facts, player, source);
        // rr:you-your.12/.14: events and eligible upgrades extend the identity;
        // rr:you-your.15/.18: allies and supports do not.
        return LabeledAbilities.IdentityPlayer(world, performer);
    }
}
