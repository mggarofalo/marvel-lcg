from . import *

# Life-Size Decoy

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Worlds.IsExpert(effect),
            toughness=1,
            change_on_event=OnEvent.NONE
        ),
        AbilityFactory.PlayersCannotThwartWhile(
            "EngagedPlayer",
            SchemeSide2,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

