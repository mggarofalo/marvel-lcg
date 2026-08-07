from . import *

# Temporal Trickery

def GetAbilities() -> Sequence['Ability']:

    def temporal_trickery_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.DiscardHandCards((1, 1), effect, most_printed_res=True)

        if faces:
            value = FacesCounter.GetPrintedResourcesIcon(faces, player.hand_cards)
            this.PlaceThreatOnSchemes("EachScheme", value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            temporal_trickery_revealed
        ),
    ]

