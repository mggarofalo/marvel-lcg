from . import *

# Speed-Metal Alloy

def GetAbilities() -> Sequence['Ability']:

    def speed_metal_alloy(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainDEFForThisAttack(2, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="SP//dr Suit"),
            speed_metal_alloy,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

