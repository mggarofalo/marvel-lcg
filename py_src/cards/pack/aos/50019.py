from . import *

# * The Douglass

def GetAbilities() -> Sequence['Ability']:

    def the_douglass(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect, ignore_crisis=True)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_douglass
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'operation'))
        .SetTarget(Scheme2, range="All"),
    ]

