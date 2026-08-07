from . import *

# The Fittest

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            when_attach_operation=lambda face, effect:
                Faces.GiveStatus([face], "Tough", effect),
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            health=5,
        ),
    ]

