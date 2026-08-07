from . import *

# Counterintelligence

def GetAbilities() -> Sequence['Ability']:

    def counterintelligence(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventThreat(3, effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.Interrupt,
            MainScheme,
            counterintelligence
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

