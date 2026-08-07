from . import *

# The Atrium A

def GetAbilities() -> Sequence['Ability']:

    def the_atrium_a_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        # Advance will do this
        # this.card.Flip(effect)
        pass


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_atrium_a_revealed
        ),
    ]

