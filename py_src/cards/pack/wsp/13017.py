from . import *

# Lie in Wait

def GetAbilities() -> Sequence['Ability']:

    def lie_in_wait(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.HeroResponse,
            None,
            "You",
            lie_in_wait
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger"),
    ]

