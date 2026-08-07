from . import *

# Silent Infiltration

def GetAbilities() -> Sequence['Ability']:

    def silent_infiltration(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        Faces.GiveStatus(effect.targets2, "Confused", effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.HeroResponse,
            "You",
            Enemy,
            silent_infiltration
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourHero", canbe_ready=True)
        .SetTarget2(Enemy, canbe_confused=True),
    ]

