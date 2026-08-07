from . import *

# Soaring Acrobatics

def GetAbilities() -> Sequence['Ability']:

    def soaring_acrobatics(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainValue(+1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            CardFinder2("AERIAL", Unit2),
            soaring_acrobatics,
            control_by="You",
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

