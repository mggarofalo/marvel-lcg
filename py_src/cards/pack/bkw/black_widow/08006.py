from . import *

# Attacrobatics

def GetAbilities() -> Sequence['Ability']:

    def attacrobatics(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        icons = message.CancelAllBoostIcons(effect)
        this.DealDamage(effect.targets, icons, effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.HeroInterrupt,
            "BoostIcons",
            attacrobatics
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Villain)
    ]

