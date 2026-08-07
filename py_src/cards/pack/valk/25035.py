from . import *

# Anticipation

def GetAbilities() -> Sequence['Ability']:

    def anticipation(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenMinionEngagePlayer(
            AbilityType.HeroInterrupt,
            None,
            "You",
            anticipation
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourHero", canbe_ready=True)
    ]

