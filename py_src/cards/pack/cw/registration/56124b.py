from . import *

# No Going Back

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CardsEnterPlayExhausted(
            Ally,
        ),
    ]

