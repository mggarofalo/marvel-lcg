using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Visits each resolving player's power branches without retaining a changed binding.
internal static class AbilityPlayerPowerPreflight
{
    internal static bool ContainsUnsupported(AbilityEffect node, AbilityAdmissionScope cast,
        bool stateMayChange, bool bindingMayChange, AbilityEffect? repeatedEffect)
    {
        int original = cast.Player;
        try
        {
            var players = AbilityEachPlayerSelection.Players(node, cast.Context.Expressions);
            foreach (int player in players)
            {
                cast.RestorePlayer(player);
                if (AbilityAdmissionResolutionPreflight.ContainsUnsupportedPower(
                    EffectBody(node), cast, stateMayChange, bindingMayChange,
                    players.Count > 1 ? EffectBody(node) : repeatedEffect)) return true;
            }
            return false;
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }
}
