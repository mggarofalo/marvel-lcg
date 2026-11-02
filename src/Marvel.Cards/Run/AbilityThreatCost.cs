using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Pays a cost of placing threat on the main scheme.</summary>
/// <remarks>
/// Printed 16124 The Beyonder's Blazer: "Place 2 threat on the main scheme and
/// spend 2 resources → discard this card." Text before the arrow is a cost that
/// "must be paid and/or resolved in full" (<c>rr:cost-arrow-icon</c>). Ruling
/// 00217fa2dcbc0f88 holds that a damage cost "cannot be prevented", so the
/// threat is placed directly, without the interrupt window an effect's threat
/// opens; a scheme it completes resolves as for any placement. The main scheme
/// is the one in the source's game area (pack:mc11:game-areas).
/// </remarks>
internal static class AbilityThreatCost
{
    internal static Card? Scheme(World world, Card source) =>
        CardReferences.TheCardIn(world, DeckType.MainSchemesArea, source);

    internal static void Pay(World world, Card source, AbilityCost.PlaceMainSchemeThreat cost,
        string trigger, List<GameEvent> events)
    {
        var scheme = Scheme(world, source)
            ?? throw new RulesNotImplementedException($"'{source.FaceId}' places threat with no main scheme in play");
        Threat.Place(world, world.Facts, world.ThreatAbilities, scheme, cost.Amount, trigger, events);
    }
}
