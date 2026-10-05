using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Board facts shared by initiation analysis and live resolution. Each query
// receives the exact domain values it reads; neither caller is exposed here.
internal static class AbilityAdmissionFacts
{
    internal static bool AlreadyInForm(
        World world, int player, string form) =>
        Forms.In(world, world.Seats[player], world.Facts, form);

    internal static bool CanAdvanceMainScheme(World world) =>
        world.TheCardIn(DeckType.MainSchemesArea) is not null
        && world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0;

    internal static bool CanDrawToPrintedHandSize(
        World world, Card source, int player)
    {
        var seat = world.Seats[player];
        int hand = seat.Hand.Cards.Count - (source.Area == seat.Hand
            && EffectiveCards.Kind(source, world.Facts) == CardKind.Event ? 1 : 0);
        return hand < world.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", world.Players);
    }

    internal static bool CanEngageTopAsMinion(
        World world, IEnumerable<int> players, long count) =>
        count > 0 && players.Any(player =>
            world.Seats[player].Deck.Cards.Count > 0
            || world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards.Count > 0);
}
