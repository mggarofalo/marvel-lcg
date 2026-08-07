from . import *

# * Red Skull's Luger

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Red Skull")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Red Skull"),
            piercing=True,
            ranged=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Red Skull")
        ),
    ]

