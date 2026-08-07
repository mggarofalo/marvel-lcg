from . import *

# Puppet Master

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            hinder="1*"
        ),
        *AbilityFactory.UnitCannotThwartTarget(
            Ally,
            cannot_thwart="This",
        ),
        *AbilityFactory.UnitCannotDefend(
            Ally,
            Villain,
            cannot_trigger_defense_ability=True
        ),
    ]

