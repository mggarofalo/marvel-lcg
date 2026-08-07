from . import *

# Homeland Security

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            check_fn=lambda effect, ui:
                Worlds.GetYourTeamSize(effect) > 1,
            hinder="2*",
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            guard=1
        ),
    ]

