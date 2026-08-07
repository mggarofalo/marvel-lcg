from . import *

# The Challengers

def GetAbilities() -> Sequence['Ability']:

    def the_challengers_revealed(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)


    return [
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "5*",
            'ratings',
            the_challengers_revealed
        ),
    ]

