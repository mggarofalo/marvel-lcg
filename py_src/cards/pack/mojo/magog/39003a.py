from . import *

# The Champion

def GetAbilities() -> Sequence['Ability']:

    def the_champion_revealed(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)


    return [
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "5*",
            'ratings',
            the_champion_revealed
        ),
    ]

