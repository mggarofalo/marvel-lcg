from . import *

# Invisibility Gear

def GetAbilities() -> Sequence['Ability']:

    def invisibility_gear(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.DoSchemeInstead(effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            Enemy,
            invisibility_gear,
            against_player="You",
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

