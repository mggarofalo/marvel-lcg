from . import *

# * Northstar: Jean-Paul Beaubier

def GetAbilities() -> Sequence['Ability']:

    def northstar(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.CancelAllBoostIcons(effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.Interrupt,
            "BoostIcons",
            northstar,
            while_attack=True
        ).SetCostFunc(CostFunc.DealDamage(1, "This")),
    ]

