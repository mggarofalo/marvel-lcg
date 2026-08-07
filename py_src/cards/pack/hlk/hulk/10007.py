from . import *

# Limitless Strength

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CheckThisCanDropPay(
            Resources("RRR"),
            spend_this_only_in_hero_form=True
        )
    ]

