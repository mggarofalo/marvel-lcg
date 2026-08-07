from . import *

# Azazel's Sword

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Azazel"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            piercing=True,
        ),
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.HeroResponse,
            "AttachedEnemy",
            DiscardThisCard
        ).SetCostFunc(CostFunc.Discard("RandomHandCard")),
    ]

