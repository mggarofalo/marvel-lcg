from . import *

# Target Acquired

def GetAbilities() -> Sequence['Ability']:

    def target_acquired(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelBoostAbility(effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.HeroResponse,
            "Ability",
            target_acquired,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

