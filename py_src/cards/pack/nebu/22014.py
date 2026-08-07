from . import *

# Justice Served

def GetAbilities() -> Sequence['Ability']:

    def justice_served(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.AfterUnitThwartAndRemoveLastThreat(
            AbilityType.HeroResponse,
            "You",
            justice_served,
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourHero", canbe_ready=True),
    ]

