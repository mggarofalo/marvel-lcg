from . import *

# Favored Weapon

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(names=["Greycrow", "Harpoon"]),
            if_cannot_attach_to=Select.From(CardFinder2("MARAUDER", Enemy), lowest_atk=True)
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            overkill=True,
            piercing=True,
            ranged=True
        ),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            Hero,
            DiscardThisCard,
            attacker="AttachedEnemy",
            take_no_damage=True,
        ),
    ]

