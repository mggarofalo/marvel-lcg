from . import *

# Adamantium Claws

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Sabretooth")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Sabretooth"),
            piercing=True,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Sabretooth")
        ),
    ]

