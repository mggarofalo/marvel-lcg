from . import *

# * Hela

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetSideSchemesNumInVictoryDisplay(effect),
            scheme=1,
            attack=1,
            health="3*",
            change_on_event=OnEvent.Victory(SchemeSide2)
        ),
        WhenDefeatedYouWinIfOdinIsNotAttachedToTheMainScheme(),
    ]

