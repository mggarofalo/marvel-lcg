from . import *

# Team Leader

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain,
            lowest_order=True,
            if_cannot_operation=ResolveMainSchemeAmbushAndAttach,
        ),
    ]

