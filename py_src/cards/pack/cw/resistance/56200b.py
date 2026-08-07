from . import *

# Going Underground

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            check_fn=lambda effect, ui:
                Worlds.GetYourTeamSize(effect) > 1,
            hinder="2*",
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "EnemyLeader",
            steady=1,
        ),
    ]

