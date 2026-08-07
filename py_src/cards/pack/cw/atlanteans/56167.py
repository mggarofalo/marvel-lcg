from . import *

# Atlantis Attacks

def GetAbilities() -> Sequence['Ability']:

    def atlantis_attacks_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.DiscardDeckTopCards("1*", effect)
        size = FacesCounter.GetDifferentTypesCount(faces)
        this.PlaceAccelerationToken(size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            atlantis_attacks_revealed
        ),
    ]

