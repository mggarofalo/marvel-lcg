from . import *

# The Infinity Stone

def GetAbilities() -> Sequence['Ability']:

    def the_infinity_stone_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        RevealExperimentalWeaponsDeckTopCard(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_infinity_stone_revealed
        ),
    ]

