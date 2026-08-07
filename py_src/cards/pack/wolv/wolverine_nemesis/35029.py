from . import *

# The Carbonadium Synthesizer

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Omega Red"),
            change_on_event=OnEvent.NONE,
        ),
    ]

