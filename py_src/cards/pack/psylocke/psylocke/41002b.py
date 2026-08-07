from . import *

# Psi-Katana

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Psylocke"),
            is_basic_attack=True,
            piercing=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Psylocke"),
            attack=1,
        ),
        *PsiKnifeKatana(Resources("R"))
    ]

