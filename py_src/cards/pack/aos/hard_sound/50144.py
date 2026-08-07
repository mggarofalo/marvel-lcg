from . import *

# Solid Sound Constructs

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Songbird"),
            if_cannot_attach_to=Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            stalwart=-1,
        ),
        AbilityFactory.AfterStatusCardPlaceOn(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            ["Confused", "Stunned"],
            DiscardThisCard
        ),
    ]

