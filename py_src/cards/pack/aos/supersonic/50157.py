from . import *

# Blasters

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="MACH-IV"),
            if_cannot_attach_to=Select.From(CardFinder(card_type=Enemy), highest_atk=True)
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            overkill=True,
            ranged=True,
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.HeroResponse,
            Identity,
            "AttachedEnemy",
            DiscardThisCard
        ).SetCost(Cost("YY"))
    ]

