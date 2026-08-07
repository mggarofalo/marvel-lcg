from . import *

# * Synch: Everett Thomas

def GetAbilities() -> Sequence['Ability']:

    def synch(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.GainValue(1, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "You",
            synch
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

