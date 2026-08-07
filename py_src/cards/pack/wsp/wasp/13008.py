from . import *

# Red Room Training

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder2("GIANT", Hero),
            retaliate=1,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity"),
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder2("TINY", Hero),
            is_basic_attack=True,
            piercing=True
        ),
    ]

