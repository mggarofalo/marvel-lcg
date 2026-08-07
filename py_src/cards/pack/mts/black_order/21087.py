from . import *

# The Black Order

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            while_face_is_in_play=CardFinder2("BLACK ORDER", Minion),
        ),
    ]

