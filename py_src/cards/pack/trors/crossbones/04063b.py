from . import *

# The Getaway

def GetAbilities() -> Sequence['Ability']:

    def the_getaway_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        RevealExperimentalWeaponsDeckTopCard(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_getaway_revealed
        ),
    ]

