from . import *

# Focusing In

def GetAbilities() -> Sequence['Ability']:

    return [
        MainSchemeWhenRevealed("Telepathy", "Telepathy"),
        *MainSchemeWhenCompleted()
    ]

