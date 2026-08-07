from . import *

# Radiation Exposure

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder2("GAMMA", Identity),
            attack=+2,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity")
        ),
        AbilityFactory.AfterUnitRecovery(
            AbilityType.ForcedResponse,
            "AttachedIdentity",
            DiscardThisCard
        ).SetCostFunc(CostFunc.Discard("YourHandCards")),
    ]

