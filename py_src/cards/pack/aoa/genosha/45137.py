from . import *

# Escaped Mutant

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "EngagedMinion",
            get_new_value=lambda effect, face:
                face.HasTrait("GENOSHA"),
            quickstrike=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.ResolveAbility(
            card_type=Environment,
            trait="SETTING")),
    ]

