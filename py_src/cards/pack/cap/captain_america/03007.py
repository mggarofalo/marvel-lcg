from . import *

# * Steve's Apartment

def GetAbilities() -> Sequence['Ability']:

    def steves_apartment(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

        this.HealthUnits(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            steves_apartment
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourIdentity")
    ]
