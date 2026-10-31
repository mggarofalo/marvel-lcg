using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityEachPlayerSelection
{
    internal static List<int> Players(AbilityEffect effect, AbilityExpressionContext context)
    {
        // EachPlayerEffects.Schedule narrows to the same players again, so a
        // mutant reading world.PlayerOrder here survives; this read is what stops
        // a source whose game area holds no player from suspending with no frame.
        var players = EachPlayerEffects.Players(context.World, context.Source).ToList();
        if (effect is not AbilityEffect.EachPlayer { DefeaterOnly: true }) return players;
        int player = AbilityCardQueries.DefeatingPlayer(context.Bindings);
        return players.Contains(player) ? [player] : [];
    }
}
