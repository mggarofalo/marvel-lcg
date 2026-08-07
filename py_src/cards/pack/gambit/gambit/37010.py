from . import *

# Molecular Acceleration

def GetAbilities() -> Sequence['Ability']:

    def molecular_acceleration(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'charge', effect)


    return [
        AbilityFactory.WhenYouSpendThisCardToPlay(
            AbilityType.HeroInterrupt,
            molecular_acceleration
        ).SetTarget(name="Gambit"),
    ]

