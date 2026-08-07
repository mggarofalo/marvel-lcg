from . import *

# Sting Operation

def GetAbilities() -> Sequence['Ability']:

    def sting_operation(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.Response,
            NonEliteMinion,
            sting_operation
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger"),
    ]

