from . import *

# Bulking Up

def GetAbilities() -> Sequence['Ability']:

    return [
        MainSchemeWhenRevealed("Super Strength", "Super Strength"),
        *MainSchemeWhenCompleted()
    ]

