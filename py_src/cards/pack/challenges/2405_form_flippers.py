from . import *

# Form Flippers

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UsingOnlyHeroes(
            ["Ant-Man", "Wasp", "Spectrum", "Vision", "Shadowcat", "Angel"]
        ),
    ]

