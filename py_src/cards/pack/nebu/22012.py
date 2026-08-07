from . import *

# * Wraith: Zak-Del

def GetAbilities() -> Sequence['Ability']:

    def wraith(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.CancelBoostAbility(effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.HeroInterrupt,
            "Ability",
            wraith,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(1, "This")),
    ]

