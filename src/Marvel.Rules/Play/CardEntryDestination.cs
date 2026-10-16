using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Determines a revealed or setup card's destination from its kind and entry instructions.</summary>
internal static class CardEntryDestination
{
    internal static Area? For(World world, ICardFacts facts, Card card, int player,
        CardEntryPlacement placement)
    {
        return EffectiveCards.Kind(card, facts) switch
        {
            CardKind.Ally when placement.SetupController is { } controller => world.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: controller),

            CardKind.Support when placement.SetupController is { } controller => world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(controller), cardOwner: controller),

            // `rr:reveal.3` and `rr:minion`: "it enters play in the play area of
            // the player revealing it. **It is considered to engage that
            // player.**" Engagement is not a flag beside the minion -- it is
            // which area the minion is in, which is what `rr:engage` describes.
            CardKind.Minion => world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)),

            // `rr:reveal.5`: "it enters play in the villain's play area."
            CardKind.EncounterSideScheme => world.AreaOf(DeckType.SideSchemesArea),

            // `rr:reveal.2`: the same place, and an area of its own because
            // `rr:environment` makes an environment "active so long as it
            // remains in play" rather than something that resolves and goes.
            CardKind.Environment => world.AreaOf(DeckType.EnvironmentArea),

            // `rr:reveal.4`: "it enters play in the play area of the player
            // revealing it."
            CardKind.Obligation => world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)),

            // `rr:attach-to`: "if a card uses the phrase 'attach to', it must
            // be attached to *(placed beneath and slightly overlapped by)* the
            // specified game element **as it enters play**." A rule about the
            // phrase rather than an ability, so it is answered here on the way
            // in and not by a "When Revealed" — `rr:when-revealed-abilities.2`
            // does not trigger one on a card put into play without being
            // revealed, and a setup attachment is exactly that.
            //
            // `rr:attach-to.3.1` is the case this does not reach: "the 'attach
            // to' phrase on a card is not resolved if another ability causes
            // that card to attach to a specific game element." An ability that
            // attaches says so with the `attachTo` node instead, and it moves
            // the card itself rather than coming through here.
            CardKind.Attachment when (placement.AttachmentTarget
                ?? world.PlacementAbilities.AttachesTo(world, card)) is { } element
                => world.AreaOf(
                    DeckType.UpgradesArea,
                    world.Cards[element].Area.PlayArea,
                    element,
                    world.Cards[element].Area.CardOwner),

            // `rr:reveal.6` and `.7`, and `rr:attach-to.3` for an attachment
            // whose phrase found nothing — "it remains in its prior state or
            // game area", which here is the table in front of the player and is
            // not in play. Step 4 discards it from there, which is the rest of
            // that clause: "if such a card cannot remain in its prior state or
            // game area, discard it."
            _ => null,
        };
    }
}
