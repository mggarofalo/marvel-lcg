using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
namespace Marvel.Cards.Run;

internal static class AbilityThreatMovementAdmission
{
    internal static bool CanMove(AbilityEffect.MoveThreat move, AbilityAdmissionScope cast) =>
        Pairs(move, cast).Length > 0;
    private static System.Collections.Immutable.ImmutableArray<AbilityThreatMovePair> Pairs(
        AbilityEffect.MoveThreat move, AbilityAdmissionScope cast) =>
        AbilityThreatMoveChoice.Pairs(Every(move.From, cast), Every(move.To, cast), cast.World,
            move.All ? 1 : Amount(move.Amount, cast));
    internal static AdmissionResolution Resolution(AbilityEffect.MoveThreat move, AbilityAdmissionScope cast) =>
        Pairs(move, cast).Select(pair => AbilityResolutionAdmission.ResolutionOfAmount(
            pair.From.Card.Tokens.GetValueOrDefault("k_threat"),
            move.All ? pair.From.Card.Tokens.GetValueOrDefault("k_threat") : Amount(move.Amount, cast)))
            .DefaultIfEmpty(AdmissionResolution.None).Max();
}
