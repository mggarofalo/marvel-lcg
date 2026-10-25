using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
namespace Marvel.Cards.Run;

internal static class AbilityThreatMovementAdmission
{
    internal static bool CanMove(AbilityEffect.MoveThreat move, AbilityAdmissionScope cast) =>
        Find(move.From, cast) is { } from && Find(move.To, cast) is { } to
        && !ReferenceEquals(from, to)
        && from.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea
        && to.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea
        && from.Tokens.GetValueOrDefault("k_threat") > 0
        && Amount(move.Amount, cast) > 0
        && cast.World.ThreatAbilities.CanRemoveThreat(cast.World, from);
    internal static AdmissionResolution Resolution(AbilityEffect.MoveThreat move, AbilityAdmissionScope cast) =>
        CanMove(move, cast)
            ? AbilityResolutionAdmission.ResolutionOfAmount(
                Find(move.From, cast)!.Tokens.GetValueOrDefault("k_threat"), Amount(move.Amount, cast))
            : AdmissionResolution.None;
}
