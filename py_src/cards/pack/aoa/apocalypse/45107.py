from . import *

# Biomorphing

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Apocalypse")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Apocalypse"),
            overkill=True
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Apocalypse")
        ),
    ]

