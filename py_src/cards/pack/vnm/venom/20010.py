from . import *

# Venom's Pistol

def GetAbilities() -> Sequence['Ability']:

    def venoms_pistol(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainValue(+1, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            CardFinder(name="Venom"),
            venoms_pistol
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourIdentity"),
    ]

