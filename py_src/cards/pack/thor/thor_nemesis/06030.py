from . import *

# Trickster

def GetAbilities() -> Sequence['Ability']:

    def trickster_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.DiscardDeckTopCards(3, effect)
        value = FacesCounter.GetDifferentTypesCount(faces)
        this.PlaceThreatOnSchemes("MainScheme", value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            trickster_revealed
        ),
    ]

