from . import *

# Informant

def GetAbilities() -> Sequence['Ability']:

    def informant(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetRemoveThreatInsteadOfPlacing(effect)


    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Interrupt,
            Minion,
            informant
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

