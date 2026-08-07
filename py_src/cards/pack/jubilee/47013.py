from . import *

# Disguise

def GetAbilities() -> Sequence['Ability']:

    def disguise(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            disguise
        ).SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Exhaust("YourIdentity"))
        .SetTarget(Scheme2)
    ]

