from . import *
from cards.pack.mojo import DiscardEachSettingAndGainSurge

# The Mojo Files

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            quickstrike=1,
        ),
        AbilityFactory.WhenAllyWouldTakeConsequentialDamage(
            Ally,
            after_attacking_minion=True,
            update_damage=-1,
        ),
        DiscardEachSettingAndGainSurge()
    ]

