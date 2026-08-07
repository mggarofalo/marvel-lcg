from . import *

# Psi-Knife

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        ),
        *PsiKnifeKatana(Resources("B")),
    ]

