from . import *

# Taking Off

def GetAbilities() -> Sequence['Ability']:

    return [
        MainSchemeWhenRevealed("Flight", "Flight"),
        *MainSchemeWhenCompleted()
    ]

