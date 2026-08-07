from . import *

# Weapons Training

def GetAbilities() -> Sequence['Ability']:

    def weapons_training(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Psylocke"),
            retaliate=1,
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            CardFinder(name="Psylocke"),
            weapons_training
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Upgrade, trait="WEAPON", canbe_ready=True, range="All", from_where=["YouControlCards"])
    ]

