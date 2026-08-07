from . import *

# Embassy Guard

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                IfAlertLevelIsHighSide(effect),
            surge=1,
            change_on_event=OnEvent.Trait(CardFinder(name="Alert Level"))
        ),
        WhenDefeatedPlaceThreatOnAlertLevel()
    ]

