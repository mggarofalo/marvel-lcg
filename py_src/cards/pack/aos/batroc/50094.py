from . import *

# Embassy Patrol

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.ThisGainIncite(
            lambda effect: IfAlertLevelIsHighSide(effect),
            incite=1,
            change_on_event=OnEvent.Trait(CardFinder(name="Alert Level"))
        ),
        WhenDefeatedPlaceThreatOnAlertLevel()
    ]

